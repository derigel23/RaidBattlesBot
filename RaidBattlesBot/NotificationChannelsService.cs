#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace RaidBattlesBot;

[UsedImplicitly]
public class NotificationChannelsService : NotificationServiceBase<NotificationChannelsServiceWorker>
{
  public NotificationChannelsService(TelemetryClient telemetryClient, IServiceProvider serviceProvider)
    : base(telemetryClient, serviceProvider, TimeSpan.FromMinutes(0.1)) { }
}

[UsedImplicitly]
public class NotificationChannelsServiceWorker : IBackgroundServiceWorker
{
  private readonly TelemetryClient myTelemetryClient;
  private readonly RaidBattlesContext myDB;
  private readonly IDictionary<long, ITelegramBotClient> myBots;

  public NotificationChannelsServiceWorker(TelemetryClient telemetryClient, RaidBattlesContext db, IDictionary<long, ITelegramBotClient> bots)
  {
    myTelemetryClient = telemetryClient;
    myDB = db;
    myBots = bots;
  }
  
  public async Task Execute(CancellationToken cancellationToken)
  {
    var toNotify = await myDB.Set<ReplyNotification>()
      .Where(n => n.MessageId == null)
      .AsTracking()
      .ToListAsync(cancellationToken);
    
      foreach (var batch in toNotify.GroupBy(n => new { n.FromChatId, n.FromMessageId }))
      {
        var counter = 0;
        using var notificationOperation = myTelemetryClient.StartOperation(
          new DependencyTelemetry(GetType().Name, null, "ChannelNotification", batch.Key.ToString()));
        foreach (var notification in batch)
        {
          try
          {
            if (notification.BotId is { } botId && myBots.TryGetValue(botId, out var bot))
            {
              var message = await bot.ForwardMessageAsync( notification.ChatId, notification.FromChatId, notification.FromMessageId, cancellationToken: cancellationToken);
              notification.MessageId = message.MessageId;
            }
            else
            {
              // unknown bot, do not try more
              notification.MessageId = -1;
            }
          }
          catch (Exception ex)
          {
            if (ex is ApiRequestException { ErrorCode: 403 })
            {
              // Do not try to notify the user again for this channel message
              notification.MessageId = -1;
            }
            else
              myTelemetryClient.TrackExceptionEx(ex, new Dictionary<string, string>
              {
                { nameof(ITelegramBotClient.BotId), notification.BotId?.ToString() ?? string.Empty }
              });
          }
          myTelemetryClient.TrackEvent("ChannelNotification", new Dictionary<string, string?>
          {
            { nameof(notification.FromChatId), notification.FromChatId.ToString() },
            { nameof(notification.FromMessageId), notification.FromMessageId.ToString() },
            { nameof(notification.BotId), notification.BotId?.ToString() },
            { nameof(notification.ChatId), notification.ChatId.ToString() },
            { nameof(notification.MessageId), notification.MessageId?.ToString() }
          });
          
          // every while and then commit processed notifications and pause
          if (++counter % 30 == 0)
          {
            await myDB.SaveChangesAsync(cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
          }
        }

        await myDB.SaveChangesAsync(cancellationToken);
      }
  }
}