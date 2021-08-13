using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
  [UsedImplicitly]
  public class FriendshipService
  {
    private readonly RaidBattlesContext myDB;

    public FriendshipService(RaidBattlesContext db)
    {
      myDB = db;
    }

    public async Task<Player> GetPlayer(User host, CancellationToken cancellationToken = default)
    {
      return await myDB.Set<Player>().FirstOrDefaultAsync(p => p.UserId == host.Id, cancellationToken);
    }

    public async Task SendCode(ITelegramBotClient bot, long userId, User host, Player player = null, CancellationToken cancellationToken = default)
    {
      player ??= await GetPlayer(host, cancellationToken);
      if (player?.FriendCode == null) return; // alarm, can't be
      var content = new StringBuilder()
        .AppendFormat("{0} Friend Code is ", host.GetLink())
        .Code((b, mode) => b.AppendFormat("{0:0000 0000 0000}", player.FriendCode))
        .AppendLine()
        .Append("Please, add him/her to your friends.")
        .ToTextMessageContent();
      await bot.SendTextMessageAsync(userId, content, cancellationToken: cancellationToken);
      var friendshipDB = myDB.Set<Friendship>();
      var friendship = await friendshipDB.SingleOrDefaultAsync(friendship =>
        friendship.Id == host.Id && friendship.FriendId == userId ||
        friendship.Id == userId && friendship.FriendId == host.Id, cancellationToken);
      if (friendship == null)
      {
        friendship = new Friendship { Id = host.Id, FriendId = userId };
        friendshipDB.Add(friendship);
      }
      friendship.Type = FriendshipType.Approved;
      await myDB.SaveChangesAsync(cancellationToken);
    }
    
    private static readonly Regex ourFriendCodeMatcher = new(@"\d{4}\s?\d{4}\s?\d{4}", RegexOptions.Compiled); 

    public async Task SetupFriendCode(ITelegramBotClient bot, User user, StringSegment text, CancellationToken cancellationToken = default)
    {
      var player = await GetPlayer(user, cancellationToken);
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
        .AppendLine($"Or use /{FriendCodeCommandHandler.COMMAND} command.")
        .Code((b, mode) => b.Append("/fc your-friend-code"));
      
      var content = builder.ToTextMessageContent();

      await bot.SendTextMessageAsync(user.Id, content, cancellationToken: cancellationToken,
        replyMarkup: replyMarkup ?? new ForceReplyMarkup { InputFieldPlaceholder = "Friend Code" });
    }
  }
}