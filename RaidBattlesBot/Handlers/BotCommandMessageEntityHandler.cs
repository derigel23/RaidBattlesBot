using System;
using System.Linq;
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
  [MessageEntityType(EntityType = MessageEntityType.BotCommand, Order = 50)]
  public class BotCommandMessageEntityHandler : IMessageEntityHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly Message myMessage;
    private readonly ITelegramBotClient myTelegramBotClient;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly PlayerCommandsHandler myPlayerCommandsHandler;

    public BotCommandMessageEntityHandler(RaidBattlesContext context, Message message, ITelegramBotClient telegramBotClient, RaidService raidService, IUrlHelper urlHelper, SetCallbackQueryHandler setCallbackQueryHandler, PlayerCommandsHandler playerCommandsHandler)
    {
      myContext = context;
      myMessage = message;
      myTelegramBotClient = telegramBotClient;
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myPlayerCommandsHandler = playerCommandsHandler;
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

        // deep linking to gym
        case "/start" when commandText.StartsWith(GeneralInlineQueryHandler.SwitchToGymParameter, StringComparison.Ordinal):
          var query = commandText.Substring(GeneralInlineQueryHandler.SwitchToGymParameter.Length);
          var pollTitle = new TextBuilder("Poll creation");
          if (int.TryParse(query, out int gymPollId))
          {
            pollTitle
              .NewLine()
              .Bold(myRaidService.GetTemporaryPoll(gymPollId)?.Title);
          }

          var content = pollTitle.ToTextMessageContent();
          await myTelegramBotClient.SendTextMessageAsync(myMessage.Chat, content, disableNotification: true, cancellationToken: cancellationToken, 
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Choose a Gym", $"{GymInlineQueryHandler.PREFIX}{query} ")));
          return false;

        // deep linking with IGN
        case "/start" when commandText.Equals(PlayerCommandsHandler.COMMAND, StringComparison.OrdinalIgnoreCase):
          return await myPlayerCommandsHandler.Process(myMessage.From, null, cancellationToken);

        case "/start" when PollEx.TryGetPollId(commandText, out var pollId, out _):
        case "/poll"  when PollEx.TryGetPollId(commandText, out pollId, out _):
          
          var existingPoll = await myContext
            .Set<Poll>()
            .Where(_ => _.Id == pollId)
            .IncludeRelatedData()
            .FirstOrDefaultAsync(cancellationToken);
          
          if (existingPoll == null)
            return false;

          pollMessage.Poll = existingPoll;
          return true;

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
              (builder, user) => user.GetLink(builder).NewLine())
            .ToTextMessageContent(disableWebPreview: true);

          await myTelegramBotClient.SendTextMessageAsync(myMessage.Chat, info, disableNotification: true,
            replyToMessageId: myMessage.MessageId, cancellationToken: cancellationToken);

          return false;

      }

      return null;
    }
  }
}