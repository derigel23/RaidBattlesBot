using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.Text)]
  public class TextMessageHandler : IMessageHandler
  {
    private readonly IEnumerable<Meta<Func<Message, IMessageEntityHandler>, MessageEntityTypeAttribute>> myMessageEntityHandlers;
    private readonly RaidBattlesContext myDb;

    public TextMessageHandler(IEnumerable<Meta<Func<Message, IMessageEntityHandler>, MessageEntityTypeAttribute>> messageEntityHandlers, RaidBattlesContext db)
    {
      myMessageEntityHandlers = messageEntityHandlers;
      myDb = db;
    }

    public async Task<bool?> Handle(Message message, PollMessage pollMessage, CancellationToken cancellationToken = default)
    {
      if (string.IsNullOrEmpty(message.Text))
        return false;

      if (message.ForwardFromChat is Chat forwarderFromChat)
      {
        var forwardedPollMessage = await myDb
          .Set<PollMessage>()
          .Where(_ => _.ChatId == forwarderFromChat.Id && _.MesssageId == message.ForwardFromMessageId)
          .IncludeRelatedData()
          .FirstOrDefaultAsync(cancellationToken);

        if (forwardedPollMessage != null)
        {
          pollMessage.Poll = forwardedPollMessage.Poll;
          return true;
        }
      }
      
      var handlers = myMessageEntityHandlers.Bind(message).ToList();
      bool? result = default;
      foreach (var entity in message.Entities ?? Enumerable.Empty<MessageEntity>())
      {
        var entityEx = new MessageEntityEx(message, entity);
        result = await HandlerExtentions<bool?>.Handle(handlers, entityEx, pollMessage, cancellationToken);
        if (result.HasValue)
          break;
      }

      return result;
    }
  }
}