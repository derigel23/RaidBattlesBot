using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using RaidBattlesBot.Handlers;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot
{
  public class FriendshipService
  {
    private readonly RaidBattlesContext myDB;

    public FriendshipService(RaidBattlesContext db)
    {
      myDB = db;
    }

    public async Task ApproveFriendship(User host, User user, CancellationToken cancellationToken = default)
    {
      var friendshipDB = myDB.Set<Friendship>();
      Expression<Func<Friendship,bool>> findFriendPredicate = friendship =>
        friendship.Id == host.Id && friendship.FriendId == user.Id ||
        friendship.Id == user.Id && friendship.FriendId == host.Id;
      var friendship = friendshipDB.Local.SingleOrDefault(findFriendPredicate.Compile()) ??
                       await friendshipDB.FirstOrDefaultAsync(findFriendPredicate, cancellationToken);
      if (friendship == null)
      {
        friendship = new Friendship { Id = host.Id, FriendId = user.Id };
        friendshipDB.Add(friendship);
      }
      friendship.Type = FriendshipType.Approved;
      await myDB.SaveChangesAsync(cancellationToken);
    }

    private static TextBuilder FormatUser(TextBuilder builder, User user, Player player = null)
    {
      builder = user.GetLink(builder);
      if (player?.Nickname is { } nickname)
      {
        builder
          .Append(" (")
          .Code(b => b.Sanitize(nickname))
          .Append(")");
      }

      return builder;
    }
    
    public async Task SendCode(ITelegramBotClient bot, User user, User host, Player hostPlayer = null, CancellationToken cancellationToken = default)
    {
      hostPlayer ??= await myDB.Set<Player>().Get(host, cancellationToken);
      if (hostPlayer?.FriendCode == null) return; // alarm, can't be
      var content = FormatUser(new TextBuilder(), host, hostPlayer)
        .Append(" Friend Code is ")
        .Code(b => b.AppendFormat("{0:0000 0000 0000}", hostPlayer.FriendCode))
        .NewLine()
        .Append("Please, add him/her to your friends.")
        .ToTextMessageContent();
      try
      {
        await bot.SendTextMessageAsync(user.Id, content, cancellationToken: cancellationToken);
      }
      finally
      {
        await ApproveFriendship(host, user, cancellationToken);
      }
    }

    public async Task AskCode(User user, ITelegramBotClient userBot, User host, ITelegramBotClient hostBot, Player userPlayer = null,  CancellationToken cancellationToken = default)
    {
      userPlayer ??= await myDB.Set<Player>().Get(user, cancellationToken);
      var userContent = FormatUser(new TextBuilder(), user, userPlayer)
        .Append(" is asking for Friendship.")
        .ToTextMessageContent();
      var userMarkup = new InlineKeyboardMarkup(new []
      {
        new []
        {
          InlineKeyboardButton.WithCallbackData("Send him/her your Friend Code",
            callbackData: FriendshipCallbackQueryHandler.Commands.SendCode(user, userBot))
        },
        new []
        {
          InlineKeyboardButton.WithCallbackData("Ask his/her Friend Code instead",
            callbackData: FriendshipCallbackQueryHandler.Commands.AskCode(user, userBot))
        },
        new []
        {
          InlineKeyboardButton.WithCallbackData("He/She is already a Friend",
            callbackData: FriendshipCallbackQueryHandler.Commands.Approve(user))
        }
      });
      await hostBot.SendTextMessageAsync(host.Id, userContent, replyMarkup: userMarkup, cancellationToken: cancellationToken);

    }

    private static readonly Regex ourFriendCodeMatcher = new(@"\d{4}\s?\d{4}\s?\d{4}", RegexOptions.Compiled); 

    public async Task SetupFriendCode(ITelegramBotClient bot, User user, StringSegment text, CancellationToken cancellationToken = default)
    {
      var player = await myDB.Set<Player>().Get(user, cancellationToken);
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
          myDB.Add(player);
        }
      
        player.FriendCode = friendCode;
        await myDB.SaveChangesAsync(cancellationToken);
      }

      IReplyMarkup replyMarkup = null;
      var builder = new TextBuilder();
      if (text.Length > 0 && friendCode == null)
      {
        builder
          .Append("Friend code is not recognized.")
          .NewLine()
          .NewLine();
      }
      
      if (player?.FriendCode is {} storedCode)
      {
        builder
          .Append("Your Friend Code is ")
          .Bold(b => b.Code(bb => bb.AppendFormat("{0:0000 0000 0000}", storedCode)))
          .NewLine()
          .NewLine();

        replyMarkup = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Clear Friend Code", 
          $"{PlayerCallbackQueryHandler.ID}:{PlayerCallbackQueryHandler.Commands.ClearFriendCode}"));
      }

      builder
        .Append("To set up your friend code reply with it to this message.").NewLine()
        .Append($"Or use /{FriendCodeCommandHandler.COMMAND} command.").NewLine()
        .Code("/fc your-friend-code");
      
      var content = builder.ToTextMessageContent();

      await bot.SendTextMessageAsync(user.Id, content, cancellationToken: cancellationToken,
        replyMarkup: replyMarkup ?? new ForceReplyMarkup { InputFieldPlaceholder = "Friend Code" });
    }
  }
}