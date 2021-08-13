using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
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
    private readonly FriendshipService myFriendshipService;

    public static class Commands
    {
      internal const string SendCodeId = "sendcode";
      internal const string AskCodeId = "askcode";
      internal const string ApproveId = "approve";
      internal const string AutoApproveId = "autoapprove";

      public static string SendCode(User user, ITelegramBotClient bot) => $"{ID}:{SendCodeId}:{user.Id}:{bot.BotId}";
      public static string AskCode(User user, ITelegramBotClient bot) => $"{ID}:{AskCodeId}:{user.Id}:{bot.BotId}";
      public static string Approve(User user) => $"{ID}:{ApproveId}:{user.Id}";
      public static string AutoApprove(Poll poll) => $"{ID}:{AutoApproveId}:{poll.Id}";
    }

    public FriendshipCallbackQueryHandler(ITelegramBotClient bot, IDictionary<long, ITelegramBotClient> bots, RaidBattlesContext db,
      FriendshipService friendshipService)
    {
      myBot = bot;
      myBots = bots;
      myDB = db;
      myFriendshipService = friendshipService;
    }
    
    public async Task<(string text, bool showAlert, string url)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data?.Split(':');
      if (callback?[0] != ID)
        return (null, false, null);

      var host = data.From;

      var player = await myFriendshipService.GetPlayer(host, cancellationToken);
      if (player?.FriendCode == null)
      {
        await myFriendshipService.SetupFriendCode(myBot, host, StringSegment.Empty, cancellationToken);
        return ("Please, specify your Friend Code first", true, null);
      }

      switch (callback.Skip(1).FirstOrDefault())
      {
        case Commands.SendCodeId
          when long.TryParse(callback.Skip(2).FirstOrDefault(), out var userId) &&
               long.TryParse(callback.Skip(3).FirstOrDefault(), out var botId):
          try
          {
            if (!myBots.TryGetValue(botId, out var bot)) bot = myBot;
            await myFriendshipService.SendCode(bot, new User { Id = userId }, host, player, cancellationToken);
            await myBot.EditMessageReplyMarkupAsync(data, InlineKeyboardMarkup.Empty(), cancellationToken);
            return ("Friend Code sent", false, null);
          }
          catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 403)
          {
            return ("User blocked personal messages for the bot.\r\nSend him/her code by yourself.", true, null);
          }
          
        case Commands.AskCodeId
          when long.TryParse(callback.Skip(2).FirstOrDefault(), out var userId) &&
               long.TryParse(callback.Skip(3).FirstOrDefault(), out var botId):
          try
          {
            if (!myBots.TryGetValue(botId, out var bot)) bot = myBot;
            await myFriendshipService.AskCode(host, myBot, new User { Id = userId }, bot, cancellationToken);
            await myBot.EditMessageReplyMarkupAsync(data, InlineKeyboardMarkup.Empty(), cancellationToken);
            return ("Friend Code asked", false, null);
          }
          catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 403)
          {
            return ("User blocked personal messages for the bot.\r\nAsk him/her for the code by yourself.", true, null);
          }
        
        case Commands.ApproveId
          when int.TryParse(callback.Skip(2).FirstOrDefault(), out var userId):

          await myFriendshipService.ApproveFriendship(host, new User { Id = userId }, cancellationToken);
          await myBot.EditMessageReplyMarkupAsync(data, InlineKeyboardMarkup.Empty(), cancellationToken);
          return ("He/She marked as already Friend.", false, null);
        
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
              try
              {
                if (vote.BotId is not { } botId || !myBots.TryGetValue(botId, out var bot))
                {
                  bot = myBot;
                }
                await myFriendshipService.SendCode(bot, vote.User, host, player, cancellationToken);
              }
              catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 403)
              {
                // personal messages banned for host - propose user to ask for FC manually
              }
            }
            
          }
          await myDB.SaveChangesAsync(cancellationToken);
          await myBot.EditMessageReplyMarkupAsync(data, InlineKeyboardMarkup.Empty(), cancellationToken);
          return ($"All invitees of `{poll.Title}` will be automatically approved.", false, null);
      }

      return (null, false, null);
    }
  }
}