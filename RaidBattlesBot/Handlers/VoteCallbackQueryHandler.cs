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
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;
using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class VoteCallbackQueryHandler : ICallbackQueryHandler
  {
    public const string ID = "vote";
    
    private readonly RaidBattlesContext myContext;
    private readonly TelemetryClient myTelemetryClient;
    private readonly ITelegramBotClientEx myBot;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly IClock myClock;
    private readonly TimeSpan myVoteTimeout;
    private readonly HashSet<int> myBlackList;

    public VoteCallbackQueryHandler(RaidBattlesContext context, TelemetryClient telemetryClient, ITelegramBotClientEx bot, RaidService raidService, IUrlHelper urlHelper, IClock clock, IOptions<BotConfiguration> options)
    {
      myContext = context;
      myTelemetryClient = telemetryClient;
      myBot = bot;
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
        if (vote.Team?.HasFlag(VoteEnum.Invitation) ?? false)
        {
          var note = await (from v in myContext.Set<Vote>() where v.PollId == pollId && v.UserId == user.Id
              join notification in myContext.Set<Notification>().Where(n => n.Type == NotificationType.RegisterNickname) on v.UserId equals notification.UserId into nn
              from n in nn.DefaultIfEmpty()
              orderby n.Modified descending
              join player in myContext.Set<Player>() on v.UserId equals player.UserId into pp
              from p in pp.DefaultIfEmpty() 
              select new { v.UserId, p.Nickname, n.Modified})
            .FirstOrDefaultAsync(cancellationToken);

          try
          {
            if (note?.Nickname == null && (note?.Modified == null || (DateTimeOffset.Now - note.Modified) > NotificationInterval))
            {
              var notificationContent = new StringBuilder()
                .Append("Please, set your in-game-nick with ")
                .Code((b, m) => b.Append("/ign your-in-game-nick"))
                .Append(" command.").ToTextMessageContent();
              await myBot.SendTextMessageAsync(user.Id, notificationContent.MessageText, notificationContent.ParseMode, cancellationToken: cancellationToken);
              await myContext.Set<Notification>()
                .AddAsync(new Notification {UserId = user.Id, Type = NotificationType.RegisterNickname}, cancellationToken);
              await myContext.SaveChangesAsync(cancellationToken);
            }
          }
          catch (Exception ex)
          {
            myTelemetryClient.TrackExceptionEx(ex);
          }
          
        }
        return (vote.Team?.GetAttributes()?.Get<DisplayAttribute>()?.Description ?? "You've voted", false, null);
      }

      return ("You've already voted.", false, null);
    }

    private static readonly TimeSpan NotificationInterval = TimeSpan.FromHours(24);
  }
}