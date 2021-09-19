using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Text;
using NodaTime.TimeZones;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.Text)]
  public class TimeZoneNotifyHandler : IMessageHandler
  {
    private readonly TelemetryClient myTelemetryClient;
    private readonly ITelegramBotClient myBot;
    private readonly RaidBattlesContext myDB;
    private readonly DateTimeZone myDateTimeZone;
    private readonly IDateTimeZoneProvider myDateTimeZoneProvider;

    public TimeZoneNotifyHandler(TelemetryClient telemetryClient, ITelegramBotClient bot, RaidBattlesContext db, DateTimeZone dateTimeZone, IDateTimeZoneProvider dateTimeZoneProvider)
    {
      myTelemetryClient = telemetryClient;
      myBot = bot;
      myDB = db;
      myDateTimeZone = dateTimeZone;
      myDateTimeZoneProvider = dateTimeZoneProvider;
    }
    
    /// processing delay for incoming messages to allow <see cref="ChosenInlineResultHandler"/> handle message first
    private static readonly TimeSpan ourDelayProcessing = TimeSpan.FromSeconds(1);

    private static readonly string[] ourClockFaces =
    {
      "🕛", "🕧", "🕐", "🕜", "🕑", "🕝", "🕒", "🕞", "🕓", "🕟", "🕔", "🕠",
      "🕕", "🕡", "🕖", "🕢", "🕗", "🕣", "🕘", "🕤", "🕙", "🕥", "🕚", "🕦"
    };

    public async Task<bool?> Handle(Message message, (UpdateType updateType, PollMessage context) context = default, CancellationToken cancellationToken = default)
    {
      if (message is { ReplyMarkup: { InlineKeyboard: {} inlineKeyboard} })
      {
        foreach (var buttons in inlineKeyboard)
        foreach (var button in buttons)
        {
          if (button.CallbackData is { } buttonCallbackData && buttonCallbackData.Split(":", 3) is {} callbackDataParts)
          {
            if (callbackDataParts.Length > 1 && callbackDataParts[0] == VoteCallbackQueryHandler.ID)
            {
              if (PollEx.TryGetPollId(callbackDataParts[1], out var pollId, out _))
              {
                var timeZoneSettings = await myDB.Set<TimeZoneSettings>().Where(settings => settings.ChatId == message.Chat.Id).ToListAsync(cancellationToken);
                if (timeZoneSettings.Count == 0) return false;
                
                await Task.Delay(ourDelayProcessing, cancellationToken);
                
                var poll = await myDB
                  .Set<Model.Poll>()
                  .Where(p => p.Id == pollId)
                  .IncludeRelatedData()
                  .Include(poll => poll.Notifications)
                  .FirstOrDefaultAsync(cancellationToken);
                if (poll?.Time is not { } datetime  ) return default; // no poll or without time

                try
                {
                 var builder = new StringBuilder();
                    
                  var culture = CultureInfo.InvariantCulture;
                  // var zoneCounties = (TzdbDateTimeZoneSource.Default.ZoneLocations ?? Enumerable.Empty<TzdbZoneLocation>())
                  //   .ToImmutableDictionary(location => location.ZoneId, location => location.CountryCode);
                  //
                  // if (zoneCounties.TryGetValue(zonedDateTime.Zone.Id, out var countryCode))
                  // {
                  //   var regionInfo = new RegionInfo(countryCode);
                  //   foreach (var cultureInfo in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
                  //   {
                  //     if (new RegionInfo(cultureInfo.LCID).GeoId == regionInfo.GeoId)
                  //     {
                  //       culture = cultureInfo;
                  //       break;
                  //     }
                  //   }
                  // }
                    
                  var pattern = ZonedDateTimePattern.Create("HH':'mm z", culture, Resolvers.StrictResolver, myDateTimeZoneProvider, ZonedDateTimePattern.GeneralFormatOnlyIso.TemplateValue);
                  var zonedDateTime = datetime.ToZonedDateTime();
                  var processedZones = new HashSet<DateTimeZone>();
                  foreach (var timeZoneId in timeZoneSettings.Select(settings => settings.TimeZone).Prepend(poll.TimeZoneId))
                  {
                    if (timeZoneId is not null && myDateTimeZoneProvider.GetZoneOrNull(timeZoneId) is { } timeZone && processedZones.Add(timeZone))
                    {
                      zonedDateTime = zonedDateTime.WithZone(timeZone);
                      var clockFace = ourClockFaces[Convert.ToInt32(zonedDateTime.ToDateTimeOffset().TimeOfDay.TotalMinutes / 30) % ourClockFaces.Length];
                      builder.Append(clockFace).Append(' ');
                      pattern.AppendFormat(zonedDateTime, builder);
                      builder.AppendLine();
                    }
                  }

                  var content = builder.ToTextMessageContent();
                  var notificationMessage = await myBot.SendTextMessageAsync(message.Chat, content, true, message.MessageId, cancellationToken: cancellationToken);
                  poll.Notifications.Add(new Notification
                  {
                    PollId = poll.Id,
                    BotId = myBot.BotId,
                    ChatId = notificationMessage.Chat.Id,
                    MessageId = notificationMessage.MessageId,
                    DateTime = notificationMessage.GetMessageDate(myDateTimeZone).ToDateTimeOffset(),
                    Type = NotificationType.TimeZone
                  });
                }
                catch (Exception ex)
                {
                  if (ex is ApiRequestException { ErrorCode: 403 })
                  {
                  }
                  else
                    myTelemetryClient.TrackExceptionEx(ex, properties: new Dictionary<string, string>
                    {
                      { nameof(ITelegramBotClient.BotId), myBot?.BotId.ToString() },
                      { "UserId", message.From?.Id.ToString() }
                    });
                }

                await myDB.SaveChangesAsync(cancellationToken);
                return true;
              }
            }
          }
        }
      }

      return default;
    }
  }
}