using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Text;
using NodaTime.TimeZones;
using RaidBattlesBot.Handlers;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot
{
  public class TimeZoneNotifyService
  {
    private readonly RaidBattlesContext myDB;
    private readonly IDateTimeZoneProvider myDateTimeZoneProvider;
    private readonly DateTimeZone myDateTimeZone;
    private readonly IClock myClock;
    private readonly ITelegramBotClient myBot;

    public TimeZoneNotifyService(RaidBattlesContext db, IDateTimeZoneProvider dateTimeZoneProvider, DateTimeZone dateTimeZone, IClock clock, ITelegramBotClient bot)
    {
      myDB = db;
      myDateTimeZoneProvider = dateTimeZoneProvider;
      myDateTimeZone = dateTimeZone;
      myClock = clock;
      myBot = bot;
    }

    private static readonly string[] ourClockFaces =
    {
      "🕛", "🕧", "🕐", "🕜", "🕑", "🕝", "🕒", "🕞", "🕓", "🕟", "🕔", "🕠",
      "🕕", "🕡", "🕖", "🕢", "🕗", "🕣", "🕘", "🕤", "🕙", "🕥", "🕚", "🕦"
    };

    public async Task<bool> ProcessPoll(ITelegramBotClient bot, long targetChatId, int? replyMessageId, Func<CancellationToken, Task<Poll>> getPoll, Func<TextBuilder> getInitialText, CancellationToken cancellationToken = default)
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
      var pattern = ZonedDateTimePattern.Create("HH':'mm z", culture, Resolvers.StrictResolver, myDateTimeZoneProvider, ZonedDateTimePattern.GeneralFormatOnlyIso.TemplateValue);
      var zonedDateTime = datetime.ToZonedDateTime();
      var processedZones = new HashSet<DateTimeZone>();
      foreach (var timeZoneId in timeZoneSettings.Select(settings => settings.TimeZone).Prepend(poll.TimeZoneId))
      {
        if (timeZoneId is not null && myDateTimeZoneProvider.GetZoneOrNull(timeZoneId) is { } timeZone && processedZones.Add(timeZone))
        {
          zonedDateTime = zonedDateTime.WithZone(timeZone);
          var clockFace = ourClockFaces[(int)(zonedDateTime.ToDateTimeOffset().TimeOfDay.TotalMinutes / 30) % ourClockFaces.Length];
          builder.Append(clockFace).Append(" ");
          pattern.AppendFormat(zonedDateTime, builder);
          builder.NewLine();
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

    public async Task<(InputTextMessageContent content, InlineKeyboardMarkup replyMarkup)> GetSettingsMessage(Chat chat, int messageId = 0, CancellationToken cancellationToken = default)
    {
      var settings = await myDB.Set<TimeZoneSettings>().Where(s => s.ChatId == chat.Id).ToListAsync(cancellationToken);
      var title = chat.Title ?? chat.Username;
      if (title == null)
      {
        title = await myBot.GetChatAsync(chat, cancellationToken) is {} ch ? ch.Title ?? ch.Username : null;
      }
      var builder = new TextBuilder("Time zone notifications for ")
        .Bold(b => b.Sanitize(title))
        .NewLine().NewLine();

      var instant = myClock.GetCurrentInstant();
      foreach (var setting in settings)
      {
        if (myDateTimeZoneProvider.GetZoneOrNull(setting.TimeZone) is {} timeZone)
        {
          builder.Append($"{timeZone.Id} ({timeZone.GetUtcOffset(instant)})").NewLine();
        }
      }
      
      if (settings.Count == 0)
      {
        builder.Append("No time zone notifications");
      }

      var encodedId = new EncodedId<long, int>(chat.Id, messageId);
      var replyMarkupButtons = new List<InlineKeyboardButton[]>
      {
        new []
        {
          chat.Type switch
          {
            ChatType.Sender or ChatType.Private =>
              InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Add", $"{TimeZoneQueryHandler.PREFIX}:{encodedId} "),
            _ =>
              InlineKeyboardButton.WithSwitchInlineQuery("Add", $"{TimeZoneQueryHandler.PREFIX}:{encodedId} ")
          }
        }
      };

      if (settings.Count > 0)
      {
        replyMarkupButtons.Add(new []
        {
          InlineKeyboardButton.WithCallbackData("Clear", $"{TimeZoneQueryHandler.PREFIX}:clear:{encodedId}")
        });
      }

      return (builder.ToTextMessageContent(), new InlineKeyboardMarkup(replyMarkupButtons));
    }

    public bool DecodeId(string encodedId, out long chatId, out int messageId)
    {
      EncodedId<long, int> decodedId = encodedId;
      (chatId, messageId) = decodedId;
      return decodedId != EncodedId<long, int>.Empty;
    }
  }
}