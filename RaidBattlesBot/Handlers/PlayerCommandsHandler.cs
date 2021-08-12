using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Handlers
{
  [BotCommand(COMMAND, "Set in-game-name (IGN)", BotCommandScopeType.AllPrivateChats, Aliases = new[] { "nick" , "nickname" }, Order = -20)]
  public class PlayerCommandsHandler : IReplyBotCommandHandler
  {
    public const string COMMAND = "ign";
    
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;
    private readonly Message myMessage;

    public PlayerCommandsHandler(RaidBattlesContext context, ITelegramBotClient bot, Message message)
    {
      myContext = context;
      myBot = bot;
      myMessage = message;
    }

    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage context = default, CancellationToken cancellationToken = default)
    {
      if (this.ShouldProcess(entity, context))
      {
        var nickname = entity.AfterValue.Trim();
        if (entity.Message != myMessage) // reply mode
        {
          nickname = myMessage.Text;
        }
        return await Process(myMessage.From, nickname.ToString(), cancellationToken);
      }

      return null;
    }

    public async Task<bool?> Process(User user, string nickname, CancellationToken cancellationToken = default)
    {
      var player = await myContext.Set<Player>().Where(p => p.UserId == user.Id).FirstOrDefaultAsync(cancellationToken);
      if (!string.IsNullOrEmpty(nickname))
      {
        if (player == null)
        {
          player = new Player
          {
            UserId = user.Id
          };
          myContext.Add(player);
        }

        player.Nickname = nickname;
        await myContext.SaveChangesAsync(cancellationToken);
      }

      IReplyMarkup replyMarkup = null;
      var builder = new StringBuilder();
      if (!string.IsNullOrEmpty(player?.Nickname))
      {
        builder
          .Append("Your IGN is ")
          .Bold((b, mode) => b.Sanitize(player.Nickname, mode))
          .AppendLine()
          .AppendLine();

        replyMarkup = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Clear IGN", $"{PlayerCallbackQueryHandler.ID}:clear"));
      }

      builder
        .AppendLine("To set up your in-game-name reply with it to this message.")
        .Append("Or use /ign command ")
          .Code((b, mode) => b.Append("/ign your-in-game-name"))
        .AppendLine(".");
      
      var content = builder.ToTextMessageContent();

      await myBot.SendTextMessageAsync(user.Id, content.MessageText, content.ParseMode, content.Entities, content.DisableWebPagePreview,
        replyMarkup: replyMarkup ?? new ForceReplyMarkup(), cancellationToken: cancellationToken);
        
      return false; // processed, but not pollMessage
    }
  }
}