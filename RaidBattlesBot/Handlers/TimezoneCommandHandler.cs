using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DelegateDecompiler;
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

namespace RaidBattlesBot.Handlers
{
  [MessageEntityType(EntityType = MessageEntityType.BotCommand)]
  public class TimezoneCommandHandler : IMessageEntityHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;
    private readonly IMemoryCache myCache;
    private readonly IClock myClock;

    public TimezoneCommandHandler(RaidBattlesContext context, ITelegramBotClient bot, IMemoryCache cache, IClock clock)
    {
      myContext = context;
      myBot = bot;
      myCache = cache;
      myClock = clock;
    }

    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage context = default, CancellationToken cancellationToken = default)
    {
      if (entity.Message.Chat.Type != ChatType.Private)
        return false;
      
      var commandText = entity.AfterValue.Trim();
      switch (entity.Command.ToString().ToLowerInvariant())
      {
        case "/timezone":
          var author = entity.Message.From;
          var settings = await myContext.Set<UserSettings>().SingleOrDefaultAsync(_ => _.UserId == author.Id, cancellationToken);

          var content = GetMessage(settings);
          var sentMessage = await myBot.SendTextMessageAsync(entity.Message.Chat, content.MessageText, content.ParseMode, content.DisableWebPagePreview,
            replyMarkup: new ReplyKeyboardMarkup(new []{ KeyboardButton.WithRequestLocation("Send your location to determine time zone") },
              resizeKeyboard: true, oneTimeKeyboard: true),
            cancellationToken: cancellationToken);

          myCache.GetOrCreate($"timezone{sentMessage.MessageId}", entry => true);
          return false; // processed, but not pollMessage

        default:
          return null;
      }
    }

    private InputTextMessageContent GetMessage(UserSettings settings)
    {
      var contentBuilder = new StringBuilder();
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
      if (message.Location is {} location && (message.ReplyToMessage?.MessageId ?? message.MessageId - 1) is { } commandMessageId && myCache.TryGetValue($"timezone{commandMessageId}", out bool _))
      {
        myCache.Remove($"timezone{commandMessageId}");  
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

      await myContext.SaveChangesAsync(cancellationToken);

      var content = GetMessage(settings);
      await myBot.SendTextMessageAsync(message.Chat, content.MessageText, content.ParseMode, content.DisableWebPagePreview,
        replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
      
      return false;
    }
  }
}