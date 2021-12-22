using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JetBrains.Annotations;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NodaTime;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;

namespace RaidBattlesBot;

[UsedImplicitly]
public class NotificationChannelBackgroundService : BackgroundService
{
  private readonly TelemetryClient myTelemetryClient;
  private readonly IClock myClock;
  private readonly RaidBattlesContext myDB;
  private readonly Channel<(NotificationChannelInfo configuration, Message message)> myChannel;

  public NotificationChannelBackgroundService(TelemetryClient telemetryClient, IClock clock, RaidBattlesContext db)
  {
    myTelemetryClient = telemetryClient;
    myClock = clock;
    myDB = db;
    myDB.Database.SetCommandTimeout(TimeSpan.FromSeconds(60));
    myChannel = Channel.CreateUnbounded<(NotificationChannelInfo configuration, Message message)>(new UnboundedChannelOptions { AllowSynchronousContinuations = true, SingleReader = true });
  }
  
  protected override async Task ExecuteAsync(CancellationToken cancellationToken)
  {
    await foreach (var (configuration, message) in myChannel.Reader.ReadAllAsync(cancellationToken))
    {
      var fromChatId = message.Chat.Id;
      var fromMessageId = message.MessageId;
      var fromUserId = message.From?.Id;

      try
      {
        // determine active users
        var now = myClock.GetCurrentInstant().ToDateTimeOffset();
        var activationStart = configuration.ActiveCheck is {} activeCheck ?
          now.Subtract(activeCheck) : DateTimeOffset.MinValue;

        var voters =
          from vote in myDB.Set<Vote>()
          where vote.Modified >= activationStart
          let rank = Sql.Ext.Rank().Over().PartitionBy(vote.UserId).OrderByDesc(vote.Modified).ToValue()
          where rank == 1
          select new { vote.BotId, vote.UserId,  };
          
        await myDB.Set<ReplyNotification>().ToLinqToDBTable()
          .Merge()
          .Using(voters)
          .On((notification, data) =>
            notification.ChatId == data.UserId &&
            notification.FromChatId == fromChatId &&
            notification.FromMessageId == fromMessageId)
          .InsertWhenNotMatched(data =>
            new ReplyNotification
            {
              BotId = data.BotId,
              ChatId = data.UserId,
              FromChatId = fromChatId,
              FromMessageId = fromMessageId,
              FromUserId = fromUserId,
              Modified = now
            })
          .MergeAsync(cancellationToken);
      }
      catch (Exception ex)
      {
        myTelemetryClient.TrackExceptionEx(ex, new Dictionary<string, string>
        {
          [nameof(fromChatId)] = fromChatId.ToString(),
          [nameof(fromMessageId)] = fromMessageId.ToString(),
          [nameof(fromUserId)] = fromUserId.ToString(),
        });
      }
    }
  }

  public ValueTask Enqueue(NotificationChannelInfo configuration, Message message, CancellationToken cancellationToken = default) =>
    myChannel.Writer.WriteAsync((configuration, message), cancellationToken);
}