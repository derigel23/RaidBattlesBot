using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NodaTime;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class VoteCallbackQueryHandler : ICallbackQueryHandler
  {
    public const string ID = "vote";
    
    private readonly RaidBattlesContext myContext;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly IClock myClock;
    private readonly TimeSpan myVoteTimeout;
    private readonly HashSet<int> myBlackList;

    public VoteCallbackQueryHandler(RaidBattlesContext context, RaidService raidService, IUrlHelper urlHelper, IClock clock, IOptions<BotConfiguration> options)
    {
      myContext = context;
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myClock = clock;
      myVoteTimeout = options.Value.VoteTimeout;
      myBlackList = options.Value.BlackList ?? new HashSet<int>(0);
    }

    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = new StringSegment(data.Data ).Split(new[] { ':' });
      if (callback.First() != ID)
        return (null, false, null);
      
      if (myBlackList.Contains(data.From.Id))
        return (null, false, null);

      Poll poll;
      if (callback.ElementAtOrDefault(1) is var pollIdSegment && PollEx.TryGetPollId(pollIdSegment, out var pollId, out var format))
      {
        poll = (await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { PollId = pollId }, myUrlHelper, format, cancellationToken))?.Poll;
      }
      else
      {
        return ("Poll is publishing. Try later.", true, null);
      }

      if (poll == null)
        return ("Poll is not found", true, null);

      var user = data.From;

      var vote = poll.Votes.SingleOrDefault(v => v.UserId == user.Id);
      if (vote == null)
      {
        poll.Votes.Add(vote = new Vote());
      }

      var now = myClock.GetCurrentInstant().ToDateTimeOffset();

      if ((now - vote.Modified) <= myVoteTimeout)
        return ($"You're voting too fast. Try again in {myVoteTimeout.TotalSeconds:0} sec", false, null);
      
      vote.User = user; // update username/firstname/lastname if necessary

      var teamAbbr = callback.ElementAt(2);
      if (!FlagEnums.TryParseFlags(teamAbbr.Value, out VoteEnum team))
        return ("Invalid vote", true, null);

      var clearTeam = team.RemoveFlags(VoteEnum.Modifiers);
      if (clearTeam == default)
        clearTeam = VoteEnum.Yes;

      vote.Team = team.HasAnyFlags(VoteEnum.Plus) && vote.Team is { } voted && voted.HasAllFlags(clearTeam) ?
        voted.CommonFlags(VoteEnum.SomePlus).IncreaseVotesCount(1) : clearTeam;

      var changed = await myContext.SaveChangesAsync(cancellationToken) > 0;
      if (changed)
      {
        await myRaidService.UpdatePoll(poll, myUrlHelper, cancellationToken);
        return (vote.Team?.GetAttributes()?.Get<DisplayAttribute>()?.Description ?? "You've voted", false, null);
      }

      return ("You've already voted.", false, null);
    }
  }
}