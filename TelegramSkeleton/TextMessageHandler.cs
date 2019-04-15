using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Microsoft.Extensions.Primitives;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  public abstract class TextMessageHandler<TContext, TResult, TMetadata> : IMessageHandler<TContext, TResult>
    where TMetadata : Attribute, IHandlerAttribute<MessageEntityEx, TContext>
  {
    private readonly ITelegramBotClient myBot;
    private readonly IEnumerable<Meta<Func<Message, IMessageEntityHandler<TContext, TResult>>, TMetadata>> myMessageEntityHandlers;

    protected TextMessageHandler(ITelegramBotClient bot, IEnumerable<Meta<Func<Message, IMessageEntityHandler<TContext, TResult>>, TMetadata>> messageEntityHandlers)
    {
      myBot = bot;
      myMessageEntityHandlers = messageEntityHandlers;
    }

    public virtual async Task<TResult> Handle(Message message, (UpdateType updateType, TContext context) _, CancellationToken cancellationToken = default)
    {
      var handlers = myMessageEntityHandlers.Bind(message).ToList();
      TResult result = default;
      foreach (var entity in message.Entities ?? Enumerable.Empty<MessageEntity>())
      {
        var entityEx = new MessageEntityEx(message, entity);
        // check bot name, if presents
        if ((entityEx.Type == MessageEntityType.BotCommand) && (entityEx.CommandBot is StringSegment commandBot) && !StringSegment.IsNullOrEmpty(commandBot))  
        {
          if (!commandBot.Equals((await myBot.GetMeAsync(cancellationToken)).Username, StringComparison.OrdinalIgnoreCase))
            continue;
        }
        result = await HandlerExtentions<TResult>.Handle(handlers, entityEx, _.context, cancellationToken).ConfigureAwait(false);
        if (!EqualityComparer<TResult>.Default.Equals(result, default))
          break;
      }

      return result;
    }
  }
}