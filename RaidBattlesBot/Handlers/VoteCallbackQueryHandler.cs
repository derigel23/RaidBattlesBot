using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NodaTime;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Team23.TelegramSkeleton;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class VoteCallbackQueryHandler : ICallbackQueryHandler
  {
    public const string ID = "vote";

    private readonly TelemetryClient myTelemetryClient;
    private readonly RaidBattlesContext myDb;
    private readonly IDictionary<long, ITelegramBotClient> myBots;
    private readonly ITelegramBotClient myBot;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly IClock myClock;
    private readonly FriendshipService myFriendshipService;
    private readonly TimeSpan myVoteTimeout;
    private readonly HashSet<long> myBlackList;

    public VoteCallbackQueryHandler(TelemetryClient telemetryClient, RaidBattlesContext db, IDictionary<long, ITelegramBotClient> bots, ITelegramBotClient bot, RaidService raidService, IUrlHelper urlHelper, IClock clock, IOptions<BotConfiguration> options,
      FriendshipService friendshipService)
    {
      myTelemetryClient = telemetryClient;
      myDb = db;
      myBots = bots;
      myBot = bot;
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myClock = clock;
      myFriendshipService = friendshipService;
      myVoteTimeout = options.Value.VoteTimeout;
      myBlackList = options.Value.BlackList ?? new HashSet<long>(0);
    }

    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = new StringSegment(data.Data).Split(new[] { ':' });
      if (callback.First() != ID)
        return (null, false, null);
      
      if (myBlackList.Contains(data.From.Id))
        return (null, false, null);

      PollMessage pollMessage;
      if (callback.ElementAtOrDefault(1) is var pollIdSegment && PollEx.TryGetPollId(pollIdSegment, out var pollId, out var format))
      {
        pollMessage = await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { BotId = myBot.BotId, PollId = pollId }, myUrlHelper, format, cancellationToken);
      }
      else
      {
        return ("Poll is publishing. Try later.", true, null);
      }

      if (pollMessage?.Poll is var poll && poll == null)
        return ("Poll is not found", true, null);

      var user = data.From;

      var teamAbbr = callback.ElementAt(2);
      if (!FlagEnums.TryParseFlags(teamAbbr.Value, true, null, out VoteEnum team, EnumFormat.HexadecimalValue, EnumFormat.Name))
        return ("Invalid vote", true, null);

      var clearTeam = team.RemoveFlags(VoteEnum.Modifiers);
      var votedTeam = clearTeam;
      var pollMode = pollMessage.PollMode ?? PollMode.Default;
      var votePollModes =  team.GetPollModes();
      switch (votePollModes.Length)
      {
        case 0:
          if (clearTeam == VoteEnum.None)
          {
            votedTeam = clearTeam = VoteEnum.Yes;
          }
          break;
        case 1:
          pollMessage.PollMode = FlagEnums.ToggleFlags(pollMode, votePollModes[0].Value);
          break;
        default:
          int enabledFlag = -1;
          for (var i = 0; i < votePollModes.Length; i++)
          {
            if (enabledFlag < 0 && pollMode.HasFlag(votePollModes[i].Value))
            {
              enabledFlag = i;
            }

            pollMode = pollMode.RemoveFlags(votePollModes[i].Value);
          }

          if (enabledFlag >= 0)
          {
            var votedPollMode = votePollModes[++enabledFlag % votePollModes.Length];
            pollMessage.PollMode = pollMode.CombineFlags(votedPollMode.Value);
            votedTeam = votedPollMode.Key.RemoveFlags(VoteEnum.Modifiers);
          }
          break;
      }

      var vote = poll.Votes.SingleOrDefault(v => v.UserId == user.Id);

      var now = myClock.GetCurrentInstant().ToDateTimeOffset();

      if ((now - vote?.Modified) <= myVoteTimeout)
        return ($"You're voting too fast. Try again in {myVoteTimeout.TotalSeconds:0} sec", false, null);
      
      if (clearTeam.HasAnyFlags())
      {
        if (vote == null)
        {
          poll.Votes.Add(vote = new Vote { BotId = myBot.BotId });
        }

        vote.User = user; // update username/firstname/lastname if necessary

        vote.Team = votedTeam = team.HasAnyFlags(VoteEnum.Plus) && vote.Team is { } voted && voted.HasAllFlags(clearTeam) ?
          voted.CommonFlags(VoteEnum.SomePlus).IncreaseVotesCount(1) : clearTeam;
      }

      var changed = await myDb.SaveChangesAsync(cancellationToken) > 0;
      if (changed)
      {
        if (votedTeam != VoteEnum.None) // real vote
        {
          await myRaidService.UpdatePoll(poll, myUrlHelper, cancellationToken);
        }
        else // some action (switch poll mode usually)
        {
          await myRaidService.UpdatePollMessage(pollMessage, myUrlHelper, cancellationToken);
        }

        // Handling invitation request
        if (votedTeam.HasFlag(VoteEnum.Invitation))
        {
          var player = await myFriendshipService.GetPlayer(user, cancellationToken);

          // request friendship from host(s)
          var hosts = poll.Votes.Where(_ => _.Team?.HasAnyFlags(VoteEnum.Host) ?? false).ToList();
          if (hosts.Count > 0)
          {
            var hostIds = hosts.ConvertAll(_ => _.UserId);
            var friendshipDB = myDb.Set<Friendship>();
            var friendships = await friendshipDB.Where(_ => hostIds.Contains(_.Id) || hostIds.Contains(_.FriendId)).ToListAsync(cancellationToken);
            var directFriendMap = friendships.ToLookup(_ => _.Id);
            var reverseFriendMap = friendships.ToLookup(_ => _.FriendId);
            var friends = directFriendMap[user.Id].Concat(reverseFriendMap[user.Id]).ToList();
            foreach (var host in hosts)
            {
              var friendship = friends.FirstOrDefault(fr => fr.Id == host.UserId || fr.FriendId == host.UserId);
              if (friendship is { Type: FriendshipType.Approved or FriendshipType.Denied }) continue;
              if (friendship == null)
              {
                friendship = new Friendship { Id = host.UserId, FriendId = user.Id, Type = FriendshipType.Awaiting };
                friendshipDB.Add(friendship);
              }
              friendship.PollId = poll.Id;

              var hostPlayer = await myFriendshipService.GetPlayer(host.User, cancellationToken);
              // auto approve
              if (hostPlayer?.AutoApproveFriendship ?? host.Team?.HasFlag(VoteEnum.AutoApproveFriend) ?? false)
              {
                try
                {
                  await myFriendshipService.SendCode(myBot, user, host.User, cancellationToken: cancellationToken);
                }
                catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 403)
                {
                  // personal messages banned for host - propose user to ask for FC manually
                }
                continue;
              }
              
              if (host.BotId is not { } botId || !myBots.TryGetValue(botId, out var bot))
              {
                bot = myBot;
              }

              try
              {
                // ask invitee instead
                if (hostPlayer?.AutoApproveFriendship is false)
                {
                  await myFriendshipService.AskCode(host.User, bot, user, myBot, hostPlayer, cancellationToken);
                  continue;
                }

                if (!(host.Team?.HasFlag(VoteEnum.AutoApproveFriendNotificationSent) ?? false))
                {
                  var pollContent = new StringBuilder("Poll ")
                    .Bold((b, m) => b.Sanitize(poll.Title, m))
                    .ToTextMessageContent();
                  var pollMarkup = new InlineKeyboardMarkup(
                    new[] { InlineKeyboardButton.WithCallbackData($"Approve all invitees", 
                      callbackData: FriendshipCallbackQueryHandler.Commands.AutoApprove(poll)) });
                  await bot.SendTextMessageAsync(host.UserId, pollContent, replyMarkup: pollMarkup, cancellationToken: cancellationToken);
                  host.Team |= VoteEnum.AutoApproveFriendNotificationSent;
                }

                await myFriendshipService.AskCode(user, myBot, host.User, bot, player, cancellationToken);
              }
              catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 403)
              {
                // personal messages banned for host - propose user to ask for FC manually
              }
              catch (Exception ex)
              {
                myTelemetryClient.TrackExceptionEx(ex, properties: new Dictionary<string, string>
                {
                  { nameof(ITelegramBotClient.BotId), bot?.BotId .ToString() },
                  { "UserId", host.UserId.ToString() }
                });
              }
            }
            await myDb.SaveChangesAsync(cancellationToken);
          }
          
          // check user's nickname
          if (string.IsNullOrEmpty(player?.Nickname))
          {
            var botInfo = await myBot.GetMeAsync(cancellationToken);
            return ("Please, set up your in-game name.", true, $"https://t.me/{botInfo.Username}?start={PlayerCommandsHandler.COMMAND}");
          }
        }
        
        return (votedTeam.GetAttributes()?.Get<DisplayAttribute>()?.Description ??
                team.GetAttributes()?.Get<DisplayAttribute>()?.Description ??
                "You've voted", false, null);
      }

      return ("You've already voted.", false, null);
    }
  }
}