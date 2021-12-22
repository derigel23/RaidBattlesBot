using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers;

[MessageType(MessageType = MessageTypeAttribute.AllMessageTypes,
  UpdateTypes = new []{ UpdateType.Message, UpdateType.EditedMessage, UpdateType.ChannelPost, UpdateType.EditedChannelPost })]
public class NotificationChannelHandler : IMessageHandler
{
  private readonly NotificationChannelBackgroundService myNotificationChannelBackgroundService;
  private readonly IReadOnlyDictionary<string, NotificationChannelInfo> myNotificationChannelsConfiguration;

  public NotificationChannelHandler(RaidBattlesContext db, NotificationChannelBackgroundService notificationChannelBackgroundService, IOptions<Dictionary<string, NotificationChannelInfo>> options)
  {
    myNotificationChannelBackgroundService = notificationChannelBackgroundService;
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
        await myNotificationChannelBackgroundService.Enqueue(configuration, message, cancellationToken);
        return true;
      }

      return false;
    }

    return null;
  }
}