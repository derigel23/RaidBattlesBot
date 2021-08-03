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
  public class PlayerCommandsHandler : IBotCommandHandler
  {
    public const string COMMAND = "ign";
    
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;

    public PlayerCommandsHandler(RaidBattlesContext context, ITelegramBotClient bot)
    {
      myContext = context;
      myBot = bot;
    }

    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage context = default, CancellationToken cancellationToken = default)
    {
      if (this.ShouldProcess(entity, context))
      {
        return await Process(entity.Message.From, entity.AfterValue.Trim().ToString(), cancellationToken);
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
    
    public async Task<bool?> HandleReply(Message message, PollMessage context, CancellationToken cancellationToken = default)
    {
      if (string.IsNullOrEmpty(message.Text) || message.Entities?.Length > 0)
        return null; // just plain text messages
      
      if (message.ReplyToMessage is { } parentMessage)
      {
        foreach (var entity in parentMessage.Entities ?? Enumerable.Empty<MessageEntity>())
        {
          if (entity.Type != MessageEntityType.BotCommand) continue;
          if (this.ShouldProcess(new MessageEntityEx(parentMessage, entity), context))
          {
            return await Process(message.From, message.Text, cancellationToken);
          }
        }
      }

      return null;
    }
  }
}