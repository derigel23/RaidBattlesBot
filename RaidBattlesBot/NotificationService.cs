using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NodaTime;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Telegram.Bot.Exceptions;

namespace RaidBattlesBot
{
  public class NotificationService : BackgroundService
  {
    private readonly TimeSpan myCheckPeriod = TimeSpan.FromSeconds(30);
    private readonly TimeSpan myErrorOffset = TimeSpan.FromSeconds(3);

    private readonly RaidBattlesContext myDB;
    private readonly RaidService myRaidService;
    private readonly TelemetryClient myTelemetryClient;
    private readonly IClock myClock;
    private readonly TimeSpan myNotificationLeadTime;

    public NotificationService(RaidBattlesContext db, RaidService raidService, TelemetryClient telemetryClient, IOptions<BotConfiguration> options, IClock clock)
    {
      myDB = db;
      myRaidService = raidService;
      myTelemetryClient = telemetryClient;
      myClock = clock;
      myNotificationLeadTime = options.Value.NotificationLeadTime;
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
      var timer = new Timer(o =>
      {
        if (!Monitor.TryEnter(this))
          return;

        try
        {
          DoWork((CancellationToken)o);
        }
        finally
        {
          Monitor.Exit(this);
        }
      }, stoppingToken, TimeSpan.Zero, myCheckPeriod);
      stoppingToken.Register(() => timer.Dispose());
      return Task.CompletedTask;
    }

    private async void DoWork(CancellationToken cancellationToken)
    {
      var nowWithLeadTime = myClock.GetCurrentInstant().ToDateTimeOffset() + myNotificationLeadTime;
      var previous = nowWithLeadTime - myCheckPeriod - myErrorOffset;
      var polls = await myDB.Set<Poll>()
        .Where(poll => poll.Time > previous && poll.Time <= nowWithLeadTime)
        .IncludeRelatedData()
        .Include(poll => poll.Notifications)
        .ToListAsync(cancellationToken);

      foreach (var poll in polls)
      {
        var pollMode = poll.AllowedVotes?.HasFlag(VoteEnum.Invitation) ?? false ? PollMode.Invitation : default;
        var alreadyNotified = poll.Notifications.Select(notification => notification.ChatId).ToHashSet();
        foreach (var pollVote in poll.Votes)
        {
          var userId = pollVote.UserId;
          if (!(pollVote.Team?.HasAnyFlags(VoteEnum.Going) ?? false)) continue;
          if (alreadyNotified.Contains(userId)) continue;
          try
          {
            var pollMessage = new PollMessage
            {
              UserId = userId,
              ChatId = userId,
              Poll = poll,
              PollId = poll.Id,
              PollMode = pollMode
            };
            var notificationMessage = await myRaidService.GetOrCreatePollAndMessage(pollMessage, null, poll.AllowedVotes, cancellationToken);
            poll.Notifications.Add(new Notification { PollId = poll.Id, ChatId = userId, DateTime = notificationMessage.Modified});
          }
          catch (Exception ex)
          {
            if (ex is ForbiddenException)
            {
              // Do not try to notify the user again for this poll
              poll.Notifications.Add(new Notification { PollId = poll.Id, ChatId = userId, DateTime = null});
              var exceptionTelemetry = new ExceptionTelemetry(ex) { SeverityLevel = SeverityLevel.Warning };
              exceptionTelemetry.Properties.Add("UserId", userId.ToString());
              myTelemetryClient.TrackException(exceptionTelemetry);
            }
            else
              myTelemetryClient.TrackExceptionEx(ex, properties: new Dictionary<string, string> { { "UserId", userId.ToString() } });
          }
        }

        await myDB.SaveChangesAsync(cancellationToken);
      }
    }
  }
}