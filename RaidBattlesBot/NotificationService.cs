#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NodaTime;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot
{
  [UsedImplicitly]
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
          DoWork((CancellationToken)o!);
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
        using var notificationOperation = myTelemetryClient.StartOperation(
          new DependencyTelemetry(GetType().Name, null, "PollNotification", poll.Id.ToString()));
        var pollMode = poll.AllowedVotes?.HasFlag(VoteEnum.Invitation) ?? false ? PollMode.DefaultWithInvitation : default;
        var alreadyNotified = poll.Notifications.Where(notification => notification.Type == NotificationType.PollTime).Select(notification => notification.ChatId).ToHashSet();
        foreach (var pollVote in poll.Votes)
        {
          var botId = pollVote.BotId;
          var userId = pollVote.UserId;
          if (!(pollVote.Team?.HasAnyFlags(VoteEnum.Notify) ?? false)) continue;
          if (alreadyNotified.Contains(userId)) continue;
          try
          {
            var pollMessage = new PollMessage
            {
              BotId = botId,
              UserId = userId,
              Chat = new Chat { Id = userId, Type = ChatType.Private },
              Poll = poll,
              PollId = poll.Id,
              PollMode = pollMode
            };
            var notificationMessage = await myRaidService.GetOrCreatePollAndMessage(pollMessage, null, poll.AllowedVotes, cancellationToken);
            var notification = new Notification
            {
              PollId = poll.Id,
              BotId = pollMessage.BotId,
              ChatId = notificationMessage.Chat.Id,
              MessageId = notificationMessage.MessageId,
              DateTime = notificationMessage.Modified,
              Type = NotificationType.PollTime
            };
            poll.Notifications.Add(notification);
            myTelemetryClient.TrackEvent("Notification", new Dictionary<string, string?>
            {
              { nameof(notificationMessage.UserId), notificationMessage.UserId?.ToString() },
              { nameof(notificationMessage.PollId), notificationMessage.PollId.ToString() },
              { nameof(notificationMessage.BotId), notificationMessage.BotId?.ToString() },
              { nameof(notificationMessage.ChatId), notificationMessage.ChatId?.ToString() },
              { nameof(notificationMessage.MessageId), notificationMessage.MessageId?.ToString() }
            });
          }
          catch (Exception ex)
          {
            if (ex is ApiRequestException { ErrorCode: 403 })
            {
              // Do not try to notify the user again for this poll
              poll.Notifications.Add(new Notification
              {
                PollId = poll.Id,
                BotId = botId,
                ChatId = userId,
                MessageId = null,
                DateTime = null,
                Type = NotificationType.PollTime
              });
            }
            else
              myTelemetryClient.TrackExceptionEx(ex, new Dictionary<string, string>
              {
                { nameof(ITelegramBotClient.BotId), botId?.ToString() ?? string.Empty },
                { "UserId", userId.ToString() }
              });
          }
        }

        await myDB.SaveChangesAsync(cancellationToken);
      }
    }
  }
}