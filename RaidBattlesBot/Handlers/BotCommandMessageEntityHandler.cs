using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot.Handlers
{
  [MessageEntityType(EntityType = MessageEntityType.BotCommand)]
  public class BotCommandMessageEntityHandler : IMessageEntityHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly Message myMessage;
    private readonly ITelegramBotClient myTelegramBotClient;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;

    public BotCommandMessageEntityHandler(RaidBattlesContext context, Message message, ITelegramBotClient telegramBotClient, RaidService raidService, IUrlHelper urlHelper)
    {
      myContext = context;
      myMessage = message;
      myTelegramBotClient = telegramBotClient;
      myRaidService = raidService;
      myUrlHelper = urlHelper;
    }

    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage pollMessage, CancellationToken cancellationToken = default)
    {
      var commandText = entity.AfterValue.Trim();
      switch (entity.Command.ToString().ToLowerInvariant())
      {
        case "/new":
          var title = commandText;
          if (StringSegment.IsNullOrEmpty(title))
            return false;
          
          pollMessage.Poll = new Poll(myMessage)
          {
            Title = title.Value
          };
          return true;

        case "/poll"  when PollEx.TryGetPollId(commandText, out var pollId, out _):
        case "/start" when PollEx.TryGetPollId(commandText, out pollId, out _):
          
          var existingPoll = await myContext
            .Set<Poll>()
            .Where(_ => _.Id == pollId)
            .IncludeRelatedData()
            .FirstOrDefaultAsync(cancellationToken);
          
          if (existingPoll == null)
            return false;

          pollMessage.Poll = existingPoll;
          return true;

        case "/set":

          IReplyMarkup replyMarkup = new InlineKeyboardMarkup(
            VoteEnumEx.AllowedVoteFormats
              .Select(flags => new[] { InlineKeyboardButton.WithCallbackData(flags.Format(new StringBuilder()).ToString(), $"set:{flags:D}") }).ToArray());

          await myTelegramBotClient.SendTextMessageAsync(myMessage.Chat, "Выберите формат голосования по умолчанию:", disableNotification: true,
            replyMarkup: replyMarkup, cancellationToken: cancellationToken);
          
          return false; // processed, but not pollMessage

        case "/help" when myMessage.Chat.Type == ChatType.Private:
          await myTelegramBotClient.SendTextMessageAsync(myMessage.Chat, "http://telegra.ph/Raid-Battles-Bot-Help-02-18", cancellationToken: cancellationToken);
          
          return false; // processed, but not pollMessage
        
        // deep linking to gym
        case "/start" when commandText.StartsWith(GeneralInlineQueryHandler.SwitchToGymParameter, StringComparison.Ordinal):
          var query = commandText.Substring(GeneralInlineQueryHandler.SwitchToGymParameter.Length);
          var pollTitle = new StringBuilder("Создание голосования");
          if (int.TryParse(query, out int gymPollId))
          {
            pollTitle
              .NewLine()
              .Bold( (builder, m) => builder.Sanitize(myRaidService.GetTemporaryPoll(gymPollId)?.Title, m));
          }

          var content = pollTitle.ToTextMessageContent();
          await myTelegramBotClient.SendTextMessageAsync(myMessage.Chat, content.MessageText, content.ParseMode, content.DisableWebPagePreview, disableNotification: true, 
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Выберите гим", $"{GymInlineQueryHandler.PREFIX}{query} ")), cancellationToken: cancellationToken);
          return false;

        case "/p" when int.TryParse(commandText, out var pollId):
          var poll = await myContext
            .Set<Poll>()
            .Where(_ => _.Id == pollId)
            .IncludeRelatedData()
            .FirstOrDefaultAsync(cancellationToken);
          if (poll == null)
            return false;

          var voters = poll.Votes.ToDictionary(vote => vote.UserId, vote => vote.User);
          var users = poll.Messages
            .Where(message => message.UserId != null)
            .Select(message => message.UserId.Value)
            .Distinct()
            .ToList();
          var unknownUsers = users.Where(u => !voters.ContainsKey(u)).ToList();
          var data = await myContext
            .Set<Vote>()
            .Where(v => unknownUsers.Contains(v.UserId))
            .GroupBy(v => v.UserId)
            .Select(vv => vv.OrderByDescending(vote => vote.Modified).First())
            .ToListAsync(cancellationToken);
          var allVoters = voters.Values.Concat(data.Select(d => d.User))
            .ToDictionary(u => u.Id, u => u);
          var info = users
            .Select(u => allVoters.TryGetValue(u, out var user) ?
                                user : new User { Id = u, FirstName = u.ToString() })
            .Aggregate(
              poll.GetDescription(myUrlHelper).NewLine().NewLine(),
              (builder, user) => builder.Append(user.GetLink()).NewLine())
            .ToTextMessageContent(disableWebPreview: true);

          await myTelegramBotClient.SendTextMessageAsync(myMessage.Chat, info.MessageText, info.ParseMode, info.DisableWebPagePreview, disableNotification: true,
            replyToMessageId: myMessage.MessageId, cancellationToken: cancellationToken);

          return false;

      }

      return null;
    }
  }
}