using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Handlers
{
  [BotCommand(COMMAND, "Set Friend Code", BotCommandScopeType.AllPrivateChats, Aliases = new[] { "tc", @"friendcode" , @"trainercode" }, Order = -18)]
  public class FriendCodeCommandHandler : IReplyBotCommandHandler
  {
    private const string COMMAND = "fc";
    
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;
    private readonly Message myMessage;

    public FriendCodeCommandHandler(RaidBattlesContext context, ITelegramBotClient bot, Message message)
    {
      myContext = context;
      myBot = bot;
      myMessage = message;
    }

    private static readonly Regex ourFriendCodeMatcher = new(@"\d{4}\s?\d{4}\s?\d{4}", RegexOptions.Compiled); 
      
    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage context = default, CancellationToken cancellationToken = default)
    {
      if (this.ShouldProcess(entity, context))
      {
        var text = entity.AfterValue.Trim();
        if (entity.Message != myMessage) // reply mode
        {
          text = myMessage.Text;
        }
        
        return await Process(myMessage.From, text, cancellationToken);
      }

      return null;
    }

    private async Task<bool?> Process(User user, StringSegment text, CancellationToken cancellationToken = default)
    {
      var player = await myContext.Set<Player>().Where(p => p.UserId == user.Id).FirstOrDefaultAsync(cancellationToken);
      long? friendCode = null;
      if (ourFriendCodeMatcher.Match(text.Buffer, text.Offset, text.Length) is { Success: true } match)
      {
        if (long.TryParse(new string(match.Value.Where(_ => !char.IsWhiteSpace(_)).ToArray()), out var code))
        {
          friendCode = code;
        }
      }

      if (friendCode != null)
      {
        if (player == null)
        {
          player = new Player
          {
            UserId = user.Id
          };
          myContext.Add(player);
        }
      
        player.FriendCode = friendCode;
        await myContext.SaveChangesAsync(cancellationToken);
      }

      IReplyMarkup replyMarkup = null;
      var builder = new StringBuilder();
      if (text.Length > 0 && friendCode == null)
      {
        builder
          .AppendLine("Friend code is not recognized.")
          .AppendLine();
      }
      
      if (player?.FriendCode is {} storedCode)
      {
        builder
          .Append("Your Friend Code is ")
          .Bold((b, mode) => b.Code((bb, m) => bb.AppendFormat("{0:0000 0000 0000}", storedCode), mode))
          .AppendLine()
          .AppendLine();

        replyMarkup = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Clear Friend Code", 
          $"{PlayerCallbackQueryHandler.ID}:{PlayerCallbackQueryHandler.Commands.ClearFriendCode}"));
      }

      builder
        .AppendLine("To set up your friend code reply with it to this message.")
        .AppendLine($"Or use /{COMMAND} command.")
        .Code((b, mode) => b.Append("/fc your-friend-code"));
      
      var content = builder.ToTextMessageContent();

      await myBot.SendTextMessageAsync(user.Id, content.MessageText, content.ParseMode, content.Entities, content.DisableWebPagePreview,
        replyMarkup: replyMarkup ?? new ForceReplyMarkup { InputFieldPlaceholder = "Friend Code" }, cancellationToken: cancellationToken);
        
      return false; // processed, but not pollMessage
    }
  }
}