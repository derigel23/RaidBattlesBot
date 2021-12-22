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
      try
      {
        // determine active users
        var now = myClock.GetCurrentInstant().ToDateTimeOffset();
        var activationStart = configuration.ActiveCheck is {} activeCheck ?
          now.Subtract(activeCheck) : DateTimeOffset.MinValue;

        var rankedVotes = from vote in myDB.Set<Vote>()
          where vote.Modified >= activationStart
          select new { vote.BotId, vote.UserId, rank = Sql.Ext.Rank().Over().PartitionBy(vote.UserId).OrderByDesc(vote.Modified).ToValue() };

        var voters = await rankedVotes
          .Where(arg => arg.rank == 1)
          .Select(arg => new { arg.BotId, arg.UserId })
          .ToListAsyncLinqToDB(cancellationToken); 

        var notifications = voters.Select(v => new ReplyNotification
        {
          BotId = v.BotId,
          ChatId = v.UserId,
          FromChatId = message.Chat.Id,
          FromMessageId = message.MessageId,
          FromUserId = message.From?.Id,
          Modified = now
        }).ToList();

        await myDB.Set<ReplyNotification>().ToLinqToDBTable()
          .Merge()
          .Using(notifications)
          .OnTargetKey()
          .InsertWhenNotMatched()
          .MergeAsync(cancellationToken);
      }
      catch (Exception ex)
      {
        myTelemetryClient.TrackExceptionEx(ex, new Dictionary<string, string>
        {
          ["ChatId"] = message.Chat.Id.ToString(),
          ["MessageId"] = message.MessageId.ToString(),
        });
      }
    }
  }

  public ValueTask Enqueue(NotificationChannelInfo configuration, Message message, CancellationToken cancellationToken = default) =>
    myChannel.Writer.WriteAsync((configuration, message), cancellationToken);
}