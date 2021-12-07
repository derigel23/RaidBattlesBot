using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeoTimeZone;
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
  [BotCommand(LOCATION_COMMAND, "Set user's home location", BotCommandScopeType.AllPrivateChats, Aliases = new[] { "timezone" }, Order = 23)]
  public class UserSettingsCommandHandler : IBotCommandHandler
  {
    private const string LOCATION_COMMAND = "location";
    
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;
    private readonly IMemoryCache myCache;
    private readonly IClock myClock;
    private readonly GeoCoder myGeoCoder;
    private readonly IDateTimeZoneProvider myDateTimeZoneProvider;

    public UserSettingsCommandHandler(RaidBattlesContext context, ITelegramBotClient bot, IMemoryCache cache, IClock clock, GeoCoder geoCoder, IDateTimeZoneProvider dateTimeZoneProvider)
    {
      myContext = context;
      myBot = bot;
      myCache = cache;
      myClock = clock;
      myGeoCoder = geoCoder;
      myDateTimeZoneProvider = dateTimeZoneProvider;
    }

    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage context = default, CancellationToken cancellationToken = default)
    {
      if (!this.ShouldProcess(entity, context)) return null;

      var author = entity.Message.From;
      var settings = await myContext.Set<UserSettings>().Get(author, cancellationToken);

      var content = await GetMessage(settings, cancellationToken);
      var sentMessage = await myBot.SendTextMessageAsync(entity.Message.Chat, content, cancellationToken: cancellationToken,
        replyMarkup: new ReplyKeyboardMarkup(KeyboardButton.WithRequestLocation("Send a location to set up your home place and timezone"))
          { ResizeKeyboard = true, OneTimeKeyboard = true });

      myCache.GetOrCreate(this[sentMessage.MessageId], _ => true);
      return false; // processed, but not pollMessage
    }

    private string this[int messageId] => $"timezone{messageId}";
      
    private async Task<InputTextMessageContent> GetMessage(UserSettings settings, CancellationToken cancellationToken = default)
    {
      var contentBuilder = new TextBuilder();
      if (settings is { Lat: {} lat, Lon: {} lon })
      {
        if (await myGeoCoder.GeoCode(new Location((double) lat, (double) lon), new StringBuilder(), 0, cancellationToken) is { Length: > 0 } geoDescription)
        {
          contentBuilder
            .Append("Your location is ")
            .Bold(b => b.Append(geoDescription))
            .NewLine();
        }
      }
      
      if (settings?.TimeZoneId is { } timeZoneId && myDateTimeZoneProvider.GetZoneOrNull(timeZoneId) is { } timeZone)
      {
        var zoneInterval = timeZone.GetZoneInterval(myClock.GetCurrentInstant());
        contentBuilder.Append("Your time zone is ")
          .Bold(builder => builder.Sanitize($"{timeZone.Id} {zoneInterval.Name} UTC{zoneInterval.WallOffset}"));
      }
      else
      {
        contentBuilder.Append("Time zone is not set.").NewLine();
      }

      return contentBuilder.ToTextMessageContent();
    }

    public async Task<bool?> ProcessLocation(Message message, CancellationToken cancellationToken = default)
    {
      if (message.Location is {} location && (message.ReplyToMessage?.MessageId ?? message.MessageId - 1) is var commandMessageId && this[commandMessageId] is {} cacheId && myCache.TryGetValue(cacheId, out bool _))
      {
        myCache.Remove(cacheId);  
      }
      else
      {
        return null;
      }

      var user = message.From ?? throw new ArgumentNullException();
      var settingsDB = myContext.Set<UserSettings>();
      var settings = await settingsDB.Get(user, cancellationToken);
      if (settings == null)
      {
        settings = new UserSettings { UserId = user.Id };
        settingsDB.Add(settings);
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
      await myBot.SendTextMessageAsync(message.Chat, content, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
      
      return false;
    }
  }
}