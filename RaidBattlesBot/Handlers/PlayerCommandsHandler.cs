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
  [MessageEntityType("ign - set in-game-name", EntityType = MessageEntityType.BotCommand)]
  public class PlayerCommandsHandler : IMessageEntityHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;

    public PlayerCommandsHandler(RaidBattlesContext context, ITelegramBotClient bot)
    {
      myContext = context;
      myBot = bot;
    }

    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage context = default, CancellationToken cancellationToken = default)
    {
      if (entity.Message.Chat.Type != ChatType.Private)
        return null; // do not process in public chats
      
      if (IsSupportedCommand(entity))
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

      await myBot.SendTextMessageAsync(user.Id, content.MessageText, content.ParseMode, content.DisableWebPagePreview,
        replyMarkup: replyMarkup ?? new ForceReplyMarkup(), cancellationToken: cancellationToken);
        
      return false; // processed, but not pollMessage
    }
    
    private static bool IsSupportedCommand(MessageEntityEx entity)
    {
      switch (entity.Command.ToString().ToLowerInvariant())
      {
        case "/ign":
        case "/nick":
        case "/nickname":
          return true;

        default:
          return false;
      }
    }

    public async Task<bool?> HandleReply(Message message, CancellationToken cancellationToken = default)
    {
      if (message.Chat.Type != ChatType.Private)
        return null; // do not check replies in public chats

      if (string.IsNullOrEmpty(message.Text) || message.Entities?.Length > 0)
        return null; // just plain text messages
      
      if (message.ReplyToMessage is { } parentMessage)
      {
        foreach (var entity in parentMessage.Entities ?? Enumerable.Empty<MessageEntity>())
        {
          if (entity.Type != MessageEntityType.BotCommand) continue;
          if (IsSupportedCommand(new MessageEntityEx(parentMessage, entity)))
          {
            return await Process(message.From, message.Text, cancellationToken);
          }
        }
      }

      return null;
    }
  }
}