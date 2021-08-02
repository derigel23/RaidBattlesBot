using System.Threading;
using System.Threading.Tasks;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [BotBotCommand(COMMAND, "Set custom poll formats", BotCommandScopeType.AllPrivateChats, Order = 50)]
  public class SetCommandHandler : IBotCommandHandler
  {
    private const string COMMAND = "set";

    private readonly ITelegramBotClient myTelegramBotClient;
    private readonly SetCallbackQueryHandler mySetCallbackQueryHandler;

    public SetCommandHandler(ITelegramBotClient telegramBotClient, SetCallbackQueryHandler setCallbackQueryHandler)
    {
      myTelegramBotClient = telegramBotClient;
      mySetCallbackQueryHandler = setCallbackQueryHandler;
    }

    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage context = default, CancellationToken cancellationToken = default)
    {
      if (!this.ShouldProcess(entity, context))
        return null;

      switch (entity.Command.ToString().ToLowerInvariant())
      {
        case "/" + COMMAND:

          var messageChat = entity.Message.Chat;
          var (setContent, setReplyMarkup) = await mySetCallbackQueryHandler.SettingsList(messageChat.Id, cancellationToken);

          await myTelegramBotClient.SendTextMessageAsync(messageChat, setContent.MessageText, setContent.ParseMode, setContent.Entities, setContent.DisableWebPagePreview, 
            disableNotification: true, replyMarkup: setReplyMarkup, cancellationToken: cancellationToken);
          
          return false; // processed, but not pollMessage
      }

      return null;
    }
  }
}