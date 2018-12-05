using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Telegram.Bot.Types;

namespace Team23.TelegramSkeleton
{
  public abstract class TextMessageHandler<TContext, TMetadata> : IMessageHandler<TContext>
    where TMetadata : Attribute, IHandlerAttribute<MessageEntityEx, TContext>
  {
    private readonly IEnumerable<Meta<Func<Message, IMessageEntityHandler<TContext>>, TMetadata>> myMessageEntityHandlers;

    protected TextMessageHandler(IEnumerable<Meta<Func<Message, IMessageEntityHandler<TContext>>, TMetadata>> messageEntityHandlers)
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