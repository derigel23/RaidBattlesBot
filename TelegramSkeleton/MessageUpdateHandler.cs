using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  [UpdateHandler(UpdateTypes = new[] { UpdateType.Message, UpdateType.EditedMessage, UpdateType.ChannelPost, UpdateType.EditedChannelPost })]
  public abstract class MessageUpdateHandler<TMessageContext, TMessageResult, TMessageMetadata> : IUpdateHandler<TMessageResult>
    where TMessageMetadata : Attribute, IHandlerAttribute<Message, (UpdateType, TMessageContext)>
  {
    private readonly IEnumerable<Meta<Func<Message, IMessageHandler<TMessageContext, TMessageResult>>, TMessageMetadata>> myMessageHandlers;

    protected MessageUpdateHandler(IEnumerable<Meta<Func<Message, IMessageHandler<TMessageContext, TMessageResult>>, TMessageMetadata>> messageHandlers)
    {
      myMessageHandlers = messageHandlers;
    }
    
    public async Task<TMessageResult> Handle(Update update, OperationTelemetry telemetry, CancellationToken cancellationToken = default)
    {
      Message message;
      var updateType = update.Type;
      switch (updateType)
      {
        case UpdateType.Message:
          message = update.Message;
          break;
        case UpdateType.EditedMessage:
          message = update.EditedMessage;
          break;
        case UpdateType.ChannelPost:
          message = update.ChannelPost;
          break;
        case UpdateType.EditedChannelPost:
          message = update.EditedChannelPost;
          break;
        
        default:
          throw new ArgumentOutOfRangeException($"Not supported update type: {update.Type} ");
      }

      telemetry.Context.User.AccountId = (message.From?.Id ?? message.ForwardFrom?.Id)?.ToString();
      telemetry.Context.User.AuthenticatedUserId = message.From?.Username ?? message.ForwardFrom?.Username;
      telemetry.Properties["uid"] = message.From?.Username ?? message.ForwardFrom?.Username;
      telemetry.Properties["messageType"] = message.Type.ToString();
      telemetry.Properties["chat"] = message.Chat.Username;

      await ProcessMessage(async (msg, context, properties, ct) =>
      {
        foreach (var property in properties)
        {
          telemetry.Properties.Add(property);
        }

        return await HandlerExtentions<TMessageResult>.Handle(myMessageHandlers.Bind(message), message, (updateType, context), ct).ConfigureAwait(false);
      }, message, cancellationToken);

      return default;
    }
    
    protected virtual TMessageContext GetMessageContext(Message message) => default;

    protected virtual Task<TMessageResult> ProcessMessage(Func<Message, TMessageContext, IDictionary<string, string>, CancellationToken, Task<TMessageResult>> processor,
      Message message,  CancellationToken cancellationToken = default)
    {
      return processor(message, GetMessageContext(message), new Dictionary<string, string>(0), cancellationToken);
    }

  }
}