using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.Text)]
  public class TextMessageHandler : TextMessageHandler<PollMessage, bool?, MessageEntityTypeAttribute>, IMessageHandler
  {
    private readonly PlayerCommandsHandler myPlayerCommandsHandler;
    private readonly RaidBattlesContext myDb;

    public TextMessageHandler(ITelegramBotClient bot, PlayerCommandsHandler playerCommandsHandler, IEnumerable<Meta<Func<Message, IMessageEntityHandler<PollMessage, bool?>>, MessageEntityTypeAttribute>> messageEntityHandlers, RaidBattlesContext db)
      : base(bot, messageEntityHandlers)
    {
      myPlayerCommandsHandler = playerCommandsHandler;
      myDb = db;
    }

    public override async Task<bool?> Handle(Message message, (UpdateType updateType, PollMessage context) _, CancellationToken cancellationToken = default)
    {
      if (string.IsNullOrEmpty(message.Text))
        return false;

      var (_, pollMessage) = _;
      if (message.ForwardFromChat is { } forwarderFromChat)
      {
        var forwardedPollMessage = await myDb
          .Set<PollMessage>()
          .Where(pm=> pm.ChatId == forwarderFromChat.Id && pm.MesssageId == message.ForwardFromMessageId)
          .IncludeRelatedData()
          .FirstOrDefaultAsync(cancellationToken);

        if (forwardedPollMessage != null)
        {
          pollMessage.Poll = forwardedPollMessage.Poll;
          return true;
        }
      }

      if (await myPlayerCommandsHandler.HandleReply(message, cancellationToken) is {} replyProcessed)
      {
        return replyProcessed;
      }
      
      return await base.Handle(message, _, cancellationToken);
    }
  }
}