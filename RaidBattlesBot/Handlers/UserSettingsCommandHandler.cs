using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeoTimeZone;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NodaTime;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using Location = GoogleMapsApi.Entities.Common.Location;

namespace RaidBattlesBot.Handlers
{
  [BotBotCommand(LOCATION_COMMAND, "Set user's home location", BotCommandScopeType.AllPrivateChats, "timezone", Order = 23)]
  public class UserSettingsCommandHandler : IBotCommandHandler
  {
    private const string LOCATION_COMMAND = "location";
    
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;
    private readonly IMemoryCache myCache;
    private readonly IClock myClock;
    private readonly GeoCoder myGeoCoder;

    public UserSettingsCommandHandler(RaidBattlesContext context, ITelegramBotClient bot, IMemoryCache cache, IClock clock, GeoCoder geoCoder)
    {
      myContext = context;
      myBot = bot;
      myCache = cache;
      myClock = clock;
      myGeoCoder = geoCoder;
    }

    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage context = default, CancellationToken cancellationToken = default)
    {
      if (!this.ShouldProcess(entity, context)) return null;

      var author = entity.Message.From;
      var settings = await myContext.Set<UserSettings>().SingleOrDefaultAsync(_ => _.UserId == author.Id, cancellationToken);

      var content = await GetMessage(settings, cancellationToken);
      var sentMessage = await myBot.SendTextMessageAsync(entity.Message.Chat, content.MessageText, content.ParseMode, content.Entities,
        content.DisableWebPagePreview,
        replyMarkup: new ReplyKeyboardMarkup(new[]
            { KeyboardButton.WithRequestLocation("Send a location to set up your home place and timezone") })
          { ResizeKeyboard = true, OneTimeKeyboard = true },
        cancellationToken: cancellationToken);

      myCache.GetOrCreate(this[sentMessage.MessageId], _ => true);
      return false; // processed, but not pollMessage
    }

    private string this[int messageId] => $"timezone{messageId}";
      
    private async Task<InputTextMessageContent> GetMessage(UserSettings settings, CancellationToken cancellationToken = default)
    {
      var contentBuilder = new StringBuilder();
      if (settings is { Lat: {} lat, Lon: {} lon })
      {
        if ((await myGeoCoder.GeoCode(new Location((double) lat, (double) lon), new StringBuilder(), 0, cancellationToken)) is {} geoDescription && geoDescription.Length > 0)
        {
          contentBuilder
            .Append("Your location is ")
            .Bold((b, mode) => b.Append(geoDescription))
            .AppendLine();
        }
      }
      
      if (settings?.TimeZoneId is { } timeZoneId && DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId) is { } timeZone)
      {
        var zoneInterval = timeZone.GetZoneInterval(myClock.GetCurrentInstant());
        contentBuilder.Append("Your time zone is ")
          .Bold((builder, mode) => builder.Sanitize($"{timeZone.Id} {zoneInterval.Name} UTC{zoneInterval.WallOffset}", mode));
      }
      else
      {
        contentBuilder.AppendLine("Time zone is not set.");
      }

      return contentBuilder.ToTextMessageContent();
    }

    public async Task<bool?> ProcessLocation(Message message, CancellationToken cancellationToken = default)
    {
      if (message.Location is {} location && (message.ReplyToMessage?.MessageId ?? message.MessageId - 1) is { } commandMessageId && this[commandMessageId] is {} cacheId && myCache.TryGetValue(cacheId, out bool _))
      {
        myCache.Remove(cacheId);  
      }
      else
      {
        return null;
      }

      var userId = message.From.Id;
      var settings = await myContext.Set<UserSettings>().SingleOrDefaultAsync(_ => _.UserId == userId, cancellationToken);
      if (settings == null)
      {
        settings = new UserSettings { UserId = userId };
        myContext.Set<UserSettings>().Add(settings);
      }
      if (TimeZoneLookup.GetTimeZone(location.Latitude, location.Longitude).Result is { } timeZoneId)
      {
        settings.TimeZoneId = timeZoneId;
      }
      else
      {
        settings.TimeZoneId = null;
      }

      settings.Lat = (decimal?) location.Latitude;      
      settings.Lon = (decimal?) location.Longitude;
      
      await myContext.SaveChangesAsync(cancellationToken);

      var content = await GetMessage(settings, cancellationToken);
      await myBot.SendTextMessageAsync(message.Chat, content.MessageText, content.ParseMode, content.Entities, content.DisableWebPagePreview,
        replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
      
      return false;
    }
  }
}