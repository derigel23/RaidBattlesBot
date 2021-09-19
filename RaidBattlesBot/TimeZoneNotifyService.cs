using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Text;
using NodaTime.TimeZones;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot
{
  public class TimeZoneNotifyService
  {
    private readonly RaidBattlesContext myDB;
    private readonly IDateTimeZoneProvider myDateTimeZoneProvider;
    private readonly DateTimeZone myDateTimeZone;

    public TimeZoneNotifyService(RaidBattlesContext db, IDateTimeZoneProvider dateTimeZoneProvider, DateTimeZone dateTimeZone)
    {
      myDB = db;
      myDateTimeZoneProvider = dateTimeZoneProvider;
      myDateTimeZone = dateTimeZone;
    }

    private static readonly string[] ourClockFaces =
    {
      "🕛", "🕧", "🕐", "🕜", "🕑", "🕝", "🕒", "🕞", "🕓", "🕟", "🕔", "🕠",
      "🕕", "🕡", "🕖", "🕢", "🕗", "🕣", "🕘", "🕤", "🕙", "🕥", "🕚", "🕦"
    };

    public async Task<bool> ProcessPoll(ITelegramBotClient bot, long targetChatId, int? replyMessageId, Func<CancellationToken, Task<Poll>> getPoll, Func<StringBuilder> getInitialText, CancellationToken cancellationToken = default)
    {
      var timeZoneSettings = await myDB.Set<TimeZoneSettings>().Where(settings => settings.ChatId == targetChatId).ToListAsync(cancellationToken);
      if (timeZoneSettings.Count == 0) 
        return false;

      var poll = await getPoll(cancellationToken);
      
      if (poll?.Time is not { } datetime)
        return default; // no poll or without time

      if (poll.Notifications.Any(notification => notification.Type == NotificationType.TimeZone && notification.ChatId == targetChatId))
        return false; // already notified
      
      var builder = getInitialText();
          
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
          var clockFace = ourClockFaces[(int)(zonedDateTime.ToDateTimeOffset().TimeOfDay.TotalMinutes / 30) % ourClockFaces.Length];
          builder.Append(clockFace).Append(' ');
          pattern.AppendFormat(zonedDateTime, builder);
          builder.AppendLine();
        }
      }

      var content = builder.ToTextMessageContent();
      var notificationMessage = await bot.SendTextMessageAsync(targetChatId, content, true, replyMessageId, cancellationToken: cancellationToken);
      poll.Notifications.Add(new Notification
      {
        PollId = poll.Id,
        BotId = bot.BotId,
        ChatId = notificationMessage.Chat.Id,
        MessageId = notificationMessage.MessageId,
        DateTime = notificationMessage.GetMessageDate(myDateTimeZone).ToDateTimeOffset(),
        Type = NotificationType.TimeZone
      });

      return true;
    }
  }
}