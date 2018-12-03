using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Handlers
{
  [Team23.TelegramSkeleton.MessageEntityType(EntityType = MessageEntityType.BotCommand)]
  public class BotCommandMessageEntityHandler : IMessageEntityHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly Message myMessage;
    private readonly ITelegramBotClient myTelegramBotClient;
    private readonly RaidService myRaidService;

    public BotCommandMessageEntityHandler(RaidBattlesContext context, Message message, ITelegramBotClient telegramBotClient, RaidService raidService)
    {
      myContext = context;
      myMessage = message;
      myTelegramBotClient = telegramBotClient;
      myRaidService = raidService;
    }

    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage pollMessage, CancellationToken cancellationToken = default)
    {
      var command = myMessage.Text.Substring(entity.Offset, entity.Length);
      var commandText = myMessage.Text.Substring(entity.Offset + entity.Length).Trim();
      switch (command)
      {
        case var _ when command.StartsWith("/new"):
          var title = commandText;
          if (string.IsNullOrEmpty(title))
            return false;
          
          pollMessage.Poll = new Poll(myMessage)
          {
            Title = title
          };
          return true;

        case var _ when command.StartsWith("/poll") && int.TryParse(commandText, out var pollId):
        case var _ when command.StartsWith("/start") && int.TryParse(commandText, out pollId):
          
          var existingPoll = await myContext
            .Set<Poll>()
            .Where(_ => _.Id == pollId)
            .IncludeRelatedData()
            .FirstOrDefaultAsync(cancellationToken);
          
          if (existingPoll == null)
            return false;

          pollMessage.Poll = existingPoll;
          return true;

        case var _ when command.StartsWith("/set"):

          IReplyMarkup replyMarkup = new InlineKeyboardMarkup(
            VoteEnumEx.AllowedVoteFormats
              .Select(flags => new[] { InlineKeyboardButton.WithCallbackData(flags.Format(new StringBuilder()).ToString(), $"set:{flags:D}") }).ToArray());

          await myTelegramBotClient.SendTextMessageAsync(myMessage.Chat, "Выберите формат голосования по умолчанию:", disableNotification: true,
            replyMarkup: replyMarkup, cancellationToken: cancellationToken);
          
          return false; // processed, but not pollMessage

        case var _ when command.StartsWith("/help") && myMessage.Chat.Type == ChatType.Private:
          await myTelegramBotClient.SendTextMessageAsync(myMessage.Chat, "http://telegra.ph/Raid-Battles-Bot-Help-02-18", cancellationToken: cancellationToken);
          
          return false; // processed, but not pollMessage
        
        // deep linking to gym
        case var _ when command.StartsWith("/start") && commandText.StartsWith(GeneralInlineQueryHandler.SwitchToGymParameter, StringComparison.Ordinal):
          var query = commandText.Substring(GeneralInlineQueryHandler.SwitchToGymParameter.Length);
          var pollTitle = new StringBuilder("Создание голосования");
          if (int.TryParse(query, out int gymPollId))
          {
            pollTitle
              .AppendLine()
              .Bold(RaidEx.ParseMode, builder => builder.Append(myRaidService.GetTemporaryPoll(gymPollId)?.Title?.Sanitize(RaidEx.ParseMode)));
          }
          await myTelegramBotClient.SendTextMessageAsync(myMessage.Chat, pollTitle.ToString(), disableNotification: true, parseMode: RaidEx.ParseMode,
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Начать привязку к гиму", GymCallbackQueryHandler.PREFIX + query)), cancellationToken: cancellationToken);
          return false;
      }

      return null;
    }
  }
}