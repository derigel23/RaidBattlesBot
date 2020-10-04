using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NodaTime;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;

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
        .ToListAsync(cancellationToken);
      
      foreach (var poll in polls)
      {
        foreach (var pollVote in poll.Votes)
        {
          if (!(pollVote.Team?.HasAnyFlags(VoteEnum.Going) ?? false))
            continue;
          
          try
          {
            var pollMessage = new PollMessage
            {
              UserId = pollVote.UserId,
              ChatId = pollVote.UserId,
              Poll = poll,
              PollId = poll.Id,
            };
            await myRaidService.GetOrCreatePollAndMessage(pollMessage, null, poll.AllowedVotes, cancellationToken);
          }
          catch (Exception ex)
          {
            myTelemetryClient.TrackException(ex);
          }
        }
      }
    }
  }
}