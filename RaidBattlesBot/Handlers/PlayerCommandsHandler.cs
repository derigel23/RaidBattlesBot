#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Handlers
{
  [BotCommand(COMMAND, "Set in-game-name (IGN)", BotCommandScopeType.AllPrivateChats, Aliases = new[] { "nick" , "nickname" }, Order = -20)]
  public class PlayerCommandsHandler : ReplyBotCommandHandler
  {
    public const string COMMAND = "ign";

    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;

    public PlayerCommandsHandler(RaidBattlesContext context, ITelegramBotClient bot, Message message) : base(message)
    {
      myContext = context;
      myBot = bot;
    }

    protected override async Task<bool?> Handle(Message message, StringSegment text, PollMessage? context = default, CancellationToken cancellationToken = default)
    {
      return await Process(message.From!, text.ToString(), cancellationToken);
    }

    public async Task<bool?> Process(User user, string nickname, CancellationToken cancellationToken = default)
    {
      var player = await myContext.Set<Player>().Get(user, cancellationToken);
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

      IReplyMarkup? replyMarkup = null;
      var builder = new TextBuilder();
      if (!string.IsNullOrEmpty(player?.Nickname))
      {
        builder
          .Append($"Your IGN is {player.Nickname:bold}")
          .NewLine()
          .NewLine();

        replyMarkup = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Clear IGN", 
          $"{PlayerCallbackQueryHandler.ID}:{PlayerCallbackQueryHandler.Commands.ClearIGN}"));
      }

      builder
        .Append($"To set up your in-game-name reply with it to this message.").NewLine()
        .Append($"Or use /{COMMAND} command.").NewLine()
        .Code("/ign your-in-game-name");
      
      await myBot.SendTextMessageAsync(user.Id, builder.ToTextMessageContent(), cancellationToken: cancellationToken,
        replyMarkup: replyMarkup ?? new ForceReplyMarkup { InputFieldPlaceholder = "in-game-name" });
        
      return false; // processed, but not pollMessage
    }
  }
}