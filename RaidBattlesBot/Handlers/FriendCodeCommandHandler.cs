using System.Threading;
using System.Threading.Tasks;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [BotCommand(COMMAND, "Set Friend Code", BotCommandScopeType.AllPrivateChats, Aliases = new[] { "tc", @"friendcode" , @"trainercode" }, Order = -18)]
  public class FriendCodeCommandHandler : IReplyBotCommandHandler
  {
    public const string COMMAND = "fc";
    
    private readonly ITelegramBotClient myBot;
    private readonly Message myMessage;
    private readonly FriendshipService myFriendshipService;

    public FriendCodeCommandHandler(ITelegramBotClient bot, Message message, FriendshipService friendshipService)
    {
      myBot = bot;
      myMessage = message;
      myFriendshipService = friendshipService;
    }

    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage context = default, CancellationToken cancellationToken = default)
    {
      if (this.ShouldProcess(entity, context))
      {
        var text = entity.AfterValue.Trim();
        if (entity.Message != myMessage) // reply mode
        {
          text = myMessage.Text;
        }
        
        await myFriendshipService.SetupFriendCode(myBot, myMessage.From, text, cancellationToken);
        
        return false; // processed, but not pollMessage
      }

      return null;
    }

  }
}