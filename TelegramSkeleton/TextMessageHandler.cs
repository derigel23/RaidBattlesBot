using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Telegram.Bot.Types;

namespace Team23.TelegramSkeleton
{
  public abstract class TextMessageHandler<TContext, TResult, TMetadata> : IMessageHandler<TContext, TResult>
    where TMetadata : Attribute, IHandlerAttribute<MessageEntityEx, TContext>
  {
    private readonly IEnumerable<Meta<Func<Message, IMessageEntityHandler<TContext, TResult>>, TMetadata>> myMessageEntityHandlers;

    protected TextMessageHandler(IEnumerable<Meta<Func<Message, IMessageEntityHandler<TContext, TResult>>, TMetadata>> messageEntityHandlers)
    {
      myMessageEntityHandlers = messageEntityHandlers;
    }

    public virtual async Task<TResult> Handle(Message message, TContext context, CancellationToken cancellationToken = default)
    {
      var handlers = myMessageEntityHandlers.Bind(message).ToList();
      TResult result = default;
      foreach (var entity in message.Entities ?? Enumerable.Empty<MessageEntity>())
      {
        var entityEx = new MessageEntityEx(message, entity);
        result = await HandlerExtentions<TResult>.Handle(handlers, entityEx, context, cancellationToken).ConfigureAwait(false);
        if (!EqualityComparer<TResult>.Default.Equals(result, default))
          break;
      }

      return result;
    }
  }
}