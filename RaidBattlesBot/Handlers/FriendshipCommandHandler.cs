using System.Threading;
using System.Threading.Tasks;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Handlers
{
  [BotCommand("friendship", "Friendship Management", BotCommandScopeType.AllPrivateChats, Order = 20)]
  public class FriendshipCommandHandler : IBotCommandHandler
  {
    private readonly ITelegramBotClient myBot;
    private readonly RaidBattlesContext myDB;

    public FriendshipCommandHandler(ITelegramBotClient bot, RaidBattlesContext db)
    {
      myBot = bot;
      myDB = db;
    }
    
    public async Task<bool?> Handle(MessageEntityEx data, PollMessage context = default, CancellationToken cancellationToken = default)
    {
      if (!this.ShouldProcess(data, context)) return null;
      
      var player = await myDB.Set<Player>().Get(data.Message.From, cancellationToken);

      var content = new TextBuilder("When someone is asking for Friendship")
        .ToTextMessageContent();

      var replyMarkup = GetInlineKeyboardMarkup(player);
      
      await myBot.SendTextMessageAsync(data.Message.Chat.Id, content, replyMarkup: replyMarkup, cancellationToken: cancellationToken);

      return false; // processed, but not pollMessage
    }

    public static InlineKeyboardMarkup GetInlineKeyboardMarkup(Player player)
    {
      return new InlineKeyboardMarkup(new[]
      {
        new[] { InlineKeyboardButton.WithCallbackData((player?.AutoApproveFriendship is true  ? '☑' : '☐') + " Auto approve request", FriendshipCallbackQueryHandler.Commands.ApproveSettings(true)) },
        new[] { InlineKeyboardButton.WithCallbackData((player?.AutoApproveFriendship is null  ? '☑' : '☐') + " Review and confirm if agree", FriendshipCallbackQueryHandler.Commands.ApproveSettings(null)) },
        new[] { InlineKeyboardButton.WithCallbackData((player?.AutoApproveFriendship is false ? '☑' : '☐') + " Ask his/her Friend Code instead", FriendshipCallbackQueryHandler.Commands.ApproveSettings(false)) },
      });
    }
  }
}