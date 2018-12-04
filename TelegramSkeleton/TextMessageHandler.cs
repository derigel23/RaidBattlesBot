using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  [MessageType(MessageType = MessageType.Text)]
  public class TextMessageHandler<TContext> : IMessageHandler<TContext>
  {
    private readonly IEnumerable<Meta<Func<Message, IMessageEntityHandler<TContext>>, MessageEntityTypeAttribute>> myMessageEntityHandlers;

    public TextMessageHandler(IEnumerable<Meta<Func<Message, IMessageEntityHandler<TContext>>, MessageEntityTypeAttribute>> messageEntityHandlers)
    {
      myMessageEntityHandlers = messageEntityHandlers;
    }

    public virtual async Task<bool?> Handle(Message message, TContext context, CancellationToken cancellationToken = default)
    {
      var handlers = myMessageEntityHandlers.Bind(message).ToList();
      bool? result = default;
      foreach (var entity in message.Entities ?? Enumerable.Empty<MessageEntity>())
      {
        var entityEx = new MessageEntityEx(message, entity);
        result = await HandlerExtentions<bool?>.Handle(handlers, entityEx, context, cancellationToken);
        if (result.HasValue)
          break;
      }

      return result;
    }
  }
}