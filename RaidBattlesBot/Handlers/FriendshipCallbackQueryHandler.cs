using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class FriendshipCallbackQueryHandler : ICallbackQueryHandler
  {
    private const string ID = "friendship";

    private readonly ITelegramBotClient myBot;
    private readonly IDictionary<long, ITelegramBotClient> myBots;
    private readonly RaidBattlesContext myDB;
    private readonly Lazy<Func<Message, FriendCodeCommandHandler>> myFriendCodeCommandHandlerFactory;

    public static class Commands
    {
      internal const string SendCodeId = "sendcode";
      internal const string AutoApproveId = "autoapprove";

      public static string SendCode(User user, ITelegramBotClient bot) => $"{ID}:{SendCodeId}:{user.Id}:{bot.BotId}";
      public static string AutoApprove(Poll poll) => $"{ID}:{AutoApproveId}:{poll.Id}";
    }

    public FriendshipCallbackQueryHandler(ITelegramBotClient bot, IDictionary<long, ITelegramBotClient> bots, RaidBattlesContext db,
      Lazy<Func<Message, FriendCodeCommandHandler>> friendCodeCommandHandlerFactory)
    {
      myBot = bot;
      myBots = bots;
      myDB = db;
      myFriendCodeCommandHandlerFactory = friendCodeCommandHandlerFactory;
    }
    
    public async Task<(string text, bool showAlert, string url)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data?.Split(':');
      if (callback?[0] != ID)
        return (null, false, null);

      var host = data.From;

      var player = await GetPlayer(host, cancellationToken);
      if (player?.FriendCode == null)
      {
        var friendCodeCommandHandler = myFriendCodeCommandHandlerFactory.Value(data.Message);
        await friendCodeCommandHandler.Process(host, StringSegment.Empty, cancellationToken);
        return ("Please, specify your Friend Code first", true, null);
      }

      switch (callback.Skip(1).FirstOrDefault())
      {
        case Commands.SendCodeId
          when long.TryParse(callback.Skip(2).FirstOrDefault(), out var userId) &&
               long.TryParse(callback.Skip(3).FirstOrDefault(), out var botId):
          if (!myBots.TryGetValue(botId, out var bot)) bot = myBot;
          await SendCode(bot, userId, host, player, cancellationToken);
          await myBot.EditMessageReplyMarkupAsync(data, InlineKeyboardMarkup.Empty(), cancellationToken);
          return ("Friend Code sent", false, null);
        
        case Commands.AutoApproveId
          when int.TryParse(callback.Skip(2).FirstOrDefault(), out var pollId):
          
          var poll = await myDB
            .Set<Poll>()
            .Where(_ => _.Id == pollId)
            .IncludeRelatedData()
            .FirstOrDefaultAsync(cancellationToken);
          if (poll == null)
            return ("Poll is publishing. Try later.", true, null);

          var hostVote = poll.Votes.FirstOrDefault(_ => _.UserId == host.Id);
          if (hostVote == null)
            return ("Poll is publishing. Try later.", true, null);

          hostVote.Team |= VoteEnum.AutoApproveFriend;
          
          // approve already awaiting requests
          foreach (var friendship in await myDB.Set<Friendship>()
            .Where(f => f.PollId == pollId && (f.Id == host.Id || f.FriendId == host.Id)).ToListAsync(cancellationToken))
          {
            friendship.Type = FriendshipType.Approved;
            if (poll.Votes.SingleOrDefault(v => (v.UserId == friendship.Id || v.UserId == friendship.FriendId) && v.UserId != host.Id) is { } vote)
            {
              if (vote.BotId is not { } botId || !myBots.TryGetValue(botId, out bot))
              {
                bot = myBot;
              }
              await SendCode(bot, vote.UserId, host, player, cancellationToken);
            }
            
          }
          await myDB.SaveChangesAsync(cancellationToken);
          await myBot.EditMessageReplyMarkupAsync(data, InlineKeyboardMarkup.Empty(), cancellationToken);
          return ($"All invitees of `{poll.Title}` will be automatically approved.", false, null);
      }

      return (null, false, null);
    }

    private async Task<Player> GetPlayer(User host, CancellationToken cancellationToken = default)
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
  }
}