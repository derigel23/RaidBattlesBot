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
using Telegram.Bot.Types.InlineQueryResults;

namespace RaidBattlesBot.Handlers
{
  [MessageEntityType(EntityType = MessageEntityType.BotCommand)]
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
        return false;
      
      var commandText = entity.AfterValue.Trim();
      switch (entity.Command.ToString().ToLowerInvariant())
      {
        case "/ign":
        case "/nick":
        case "/nickname":
          var author = entity.Message.From;
          var player = await myContext.Set<Player>().Where(p => p.UserId == author.Id).FirstOrDefaultAsync(cancellationToken);
          var nickname = commandText.ToString();
          if (string.IsNullOrEmpty(nickname))
          {
            if (player != null)
            {
              myContext.Remove(player);
            }
          }
          else
          {
            if (player == null)
            {
              player = new Player
              {
                UserId = author.Id
              };
              myContext.Add(player);
            }

            player.Nickname = nickname;
          }
          await myContext.SaveChangesAsync(cancellationToken);
          
          InputTextMessageContent content;
          if (string.IsNullOrEmpty(nickname))
          {
            content = new StringBuilder()
              .Append("Your IGN is cleared.\r\nUse ")
              .Code((b, m) => b.Append("/ign your-in-game-name"))
              .Append(" command to set it up.")
              .ToTextMessageContent();
          }
          else
          {
            content = new StringBuilder("Your IGN ").Code((b, mode) => b.Sanitize(nickname, mode)).Append(" is recorded.").ToTextMessageContent();
          }
          await myBot.SendTextMessageAsync(entity.Message.Chat, content.MessageText, content.ParseMode, content.DisableWebPagePreview, cancellationToken: cancellationToken);
          
          return false; // processed, but not pollMessage

        default:
          return null;
      }
    }
  }
}