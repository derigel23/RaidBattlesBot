using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NodaTime;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers;

[MessageType(MessageType = MessageTypeAttribute.AllMessageTypes,
  UpdateTypes = new []{ UpdateType.Message, UpdateType.EditedMessage, UpdateType.ChannelPost, UpdateType.EditedChannelPost })]
public class NotificationChannelHandler : IMessageHandler
{
  private readonly RaidBattlesContext myDB;
  private readonly IClock myClock;
  private readonly IReadOnlyDictionary<string, NotificationChannelInfo> myNotificationChannelsConfiguration;

  public NotificationChannelHandler(RaidBattlesContext db, IClock clock, IOptions<Dictionary<string, NotificationChannelInfo>> options)
  {
    myDB = db;
    myClock = clock;
    myNotificationChannelsConfiguration = options.Value;
  }
  
  public async Task<bool?> Handle(Message message, (UpdateType updateType, PollMessage context) context = default, CancellationToken cancellationToken = default)
  {
    // check notification channels (settings doesn't support long as key)
    if (myNotificationChannelsConfiguration.TryGetValue(message.Chat.Id.ToString(CultureInfo.InvariantCulture), out var configuration))
    {
      var process = true;
      // check notification channel tags, if specified
      if (configuration.Tags is { Length: > 0 } tags)
      {
        var tagsSet = new HashSet<StringSegment>(tags.Select(tag => new StringSegment(tag)), StringSegmentComparer.OrdinalIgnoreCase);
        process = message.Entities?
          .Where(e => e.Type == MessageEntityType.Hashtag && tagsSet.Contains(new MessageEntityEx(message, e).Value))
          .Any() ?? false;
      }

      if (process)
      {
        // determine active users
        var now = myClock.GetCurrentInstant().ToDateTimeOffset();
        var activationStart = configuration.ActiveCheck is {} activeCheck ?
          now.Subtract(activeCheck) : DateTimeOffset.MinValue;
          
        var voters = await myDB.Set<Vote>()
          .Where(v => v.Modified > activationStart)
          .GroupBy(v => v.UserId, (l, votes) => votes.OrderByDescending(vv => vv.Modified).First())
          .ToListAsync(cancellationToken);

        var notifications = voters.Select(v => new ReplyNotification
        {
          BotId = v.BotId,
          ChatId = v.UserId,
          FromChatId = message.Chat.Id,
          FromMessageId = message.MessageId,
          FromUserId = message.From?.Id,
          Modified = now
        }).ToList();
          
        await myDB.BulkInsertOrUpdateAsync(notifications, cancellationToken: cancellationToken);
        await myDB.SaveChangesAsync(cancellationToken);
          
        return true;
      }

      return false;
    }

    return null;
  }
}