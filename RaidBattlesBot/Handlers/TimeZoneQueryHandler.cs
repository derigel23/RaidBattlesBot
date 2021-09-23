using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NodaTime;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Handlers
{
  [InlineQueryHandler(QueryPattern = PATTERN)]
  [CallbackQueryHandler(DataPrefix = PREFIX)]
  public class TimeZoneQueryHandler : IInlineQueryHandler, IChosenInlineResultHandler, ICallbackQueryHandler
  {
    public const string PREFIX = "/tz";
    private const string PATTERN = @"(^|\s+)" + PREFIX + @":(?<Id>\S+)";

    private readonly ITelegramBotClientEx myBot;
    private readonly IDateTimeZoneProvider myDateTimeZoneProvider;
    private readonly IClock myClock;
    private readonly IUrlHelper myUrlHelper;
    private readonly RaidBattlesContext myDB;
    private readonly TimeZoneNotifyService myTimeZoneNotifyService;
    private readonly TimeZoneService myTimeZoneService;
    private readonly HashSet<long> mySuperAdministrators;

    public TimeZoneQueryHandler(ITelegramBotClientEx bot, IDateTimeZoneProvider dateTimeZoneProvider, IClock clock, IUrlHelper urlHelper, RaidBattlesContext db, IOptions<BotConfiguration> options, TimeZoneNotifyService timeZoneNotifyService, TimeZoneService timeZoneService)
    {
      myBot = bot;
      myDateTimeZoneProvider = dateTimeZoneProvider;
      myClock = clock;
      myUrlHelper = urlHelper;
      myDB = db;
      myTimeZoneNotifyService = timeZoneNotifyService;
      myTimeZoneService = timeZoneService;
      mySuperAdministrators = options.Value?.SuperAdministrators ?? new HashSet<long>(0);
    }

    private const int PAGE_SIZE = 20;
    
    public async Task<bool?> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var results = new List<InlineQueryResult>();

      var query = data.Query;
      string encodedId = null;
      if (Regex.Match(query, PATTERN) is { Success: true } match)
      {
        encodedId = match.Groups["Id"].Value;
        query = query.Remove(match.Index, match.Length);
      }
      query = query.Trim();

      var matchedZones = myDateTimeZoneProvider.Ids.Where(id =>
      {
        // check tz id
        if (id.Contains(query, StringComparison.OrdinalIgnoreCase))
          return true;
        
        // check Region name
        return myTimeZoneService.TryGetRegion(id, out var region) && (
          region.EnglishName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
          region.NativeName.Contains(query, StringComparison.OrdinalIgnoreCase));
      }).ToList();

      InlineKeyboardMarkup replyMarkup = null;
      if (myTimeZoneNotifyService.DecodeId(encodedId, out var chatId, out var messageId))
      {
        (_, replyMarkup) = await myTimeZoneNotifyService.GetSettingsMessage(new Chat { Id = chatId, Type = ChatType.Sender }, messageId, cancellationToken);
      }
      var instant = myClock.GetCurrentInstant();
      int.TryParse(data.Offset, out var offset);
      for (var index = offset; index < matchedZones.Count && index < offset + PAGE_SIZE; index++)
      {
        var id = matchedZones[index];
        if (myDateTimeZoneProvider.GetZoneOrNull(id) is { } timeZone)
        {
          var title = $"{timeZone.Id} ({timeZone.GetUtcOffset(instant)})";
          var builder = new StringBuilder()
            .Append(title)
            .AppendLine("\x00A0"); // trailing space is necessary to allow edit it further to the same message

          string countryString = null;
          if (myTimeZoneService.TryGetRegion(timeZone.Id, out var country))
          {
            countryString = $"{country.EnglishName} {country.NativeName}";
            builder.AppendLine(countryString);
          }

          results.Add(new InlineQueryResultArticle($"{PREFIX}:{encodedId}:{timeZone.Id}", title, builder.ToTextMessageContent())
          {
            Description = countryString,
            ReplyMarkup = replyMarkup
          });
        }
      }

      string nextOffset = default;
      if (results.Count == 0 && offset == 0)
      {
        results.Add(new InlineQueryResultArticle("NothingFound", "Nothing found", 
          new StringBuilder($"Nothing found by request ").Code((builder, mode) => builder.Sanitize(query, mode)).ToTextMessageContent())
        {
          Description = $"Request {query}",
          ThumbUrl = myUrlHelper.AssetsContent(@"static_assets/png/btn_close_normal.png").AbsoluteUri
        });
      }
      else
      {
        nextOffset = (offset + PAGE_SIZE).ToString();
      }

      await myBot.AnswerInlineQueryWithValidationAsync(data.Id, results, nextOffset: nextOffset, cacheTime: 0, isPersonal: true, cancellationToken: cancellationToken);

      return true;
    }

    public async Task<bool?> Handle(ChosenInlineResult data, object context = default, CancellationToken cancellationToken = default)
    {
      var resultParts = data.ResultId.Split(':');
      switch (resultParts[0])
      {
        case PREFIX:
          if (myTimeZoneNotifyService.DecodeId(resultParts.Skip(1).FirstOrDefault(), out var chatId, out var messageId) &&
              resultParts.Skip(2).FirstOrDefault() is {} timeZoneId && myDateTimeZoneProvider.GetZoneOrNull(timeZoneId) is {} timeZone)
          {
            if (!await CheckRights(chatId, data.From, cancellationToken))
            {
              await myBot.EditMessageTextAsync(data.InlineMessageId, new InputTextMessageContent("You have no rights"), cancellationToken: cancellationToken);
              return false;
            }
            
            var settings = myDB.Set<TimeZoneSettings>();
            if (await settings.Where(s => s.ChatId == chatId && s.TimeZone == timeZoneId).FirstOrDefaultAsync(cancellationToken) == null)
            {
              settings.Add(new TimeZoneSettings { ChatId = chatId, TimeZone = timeZoneId });
              await myDB.SaveChangesAsync(cancellationToken);
            }
            
            var (content, replyMarkup) = await myTimeZoneNotifyService.GetSettingsMessage(new Chat { Id = chatId, Type = ChatType.Sender }, cancellationToken: cancellationToken);

            await myBot.EditMessageTextAsync(data.InlineMessageId, content, replyMarkup, cancellationToken);

            if (messageId != 0)
            {
              await myBot.DeleteMessageAsync(chatId, messageId, cancellationToken);
            }

            return true;
          }
          break;
      }

      return null;
    }

    private async Task<bool> CheckRights(ChatId chatId, User user, CancellationToken cancellationToken)
    {
      if (chatId.Identifier == user.Id)
        return true;
      
      if (mySuperAdministrators.Contains(user.Id))
        return true;

      return (await myBot.GetChatMemberAsync(chatId, user.Id, cancellationToken)).Status switch
      {
        ChatMemberStatus.Creator => true,
        ChatMemberStatus.Administrator => true,
        _ => false
      };
    }

    public async Task<(string text, bool showAlert, string url)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != PREFIX)
        return (null, false, null);

      switch (callback.Skip(1).FirstOrDefault())
      {
        case "clear" when myTimeZoneNotifyService.DecodeId(callback.Skip(2).FirstOrDefault(), out var chatId, out var messageId):
          if (!await CheckRights(chatId, data.From, cancellationToken))
          {
            return ("You have no rights", true, null);
          }
          await myDB.Set<TimeZoneSettings>().Where(s => s.ChatId == chatId).BatchDeleteAsync(cancellationToken);
          var (content, replyMarkup) = await myTimeZoneNotifyService.GetSettingsMessage(new Chat { Id = chatId, Type = ChatType.Sender }, messageId, cancellationToken);
          await myBot.EditMessageTextAsync(data, content, replyMarkup, cancellationToken);
          
          return ("All time zone notifications removed", false, null);
      }

      return (null, false, null);
    }
  }
}