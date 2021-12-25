#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [BotCommand(COMMAND, "Set Friend Code", BotCommandScopeType.AllPrivateChats, Aliases = new[] { "tc", @"friendcode" , @"trainercode" }, Order = -18)]
  public class FriendCodeCommandHandler : ReplyBotCommandHandler
  {
    public const string COMMAND = "fc";
    
    private readonly ITelegramBotClient myBot;
    private readonly FriendshipService myFriendshipService;

    public FriendCodeCommandHandler(ITelegramBotClient bot, Message message, FriendshipService friendshipService) : base(message)
    {
      myBot = bot;
      myFriendshipService = friendshipService;
    }

    protected override async Task<bool?> Handle(Message message, StringSegment text, PollMessage? context = default, CancellationToken cancellationToken = default)
    {
      await myFriendshipService.SetupFriendCode(myBot, message.From, text, cancellationToken);
        
      return false; // processed, but not pollMessage
    }
  }
}