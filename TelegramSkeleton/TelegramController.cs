using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  public abstract class TelegramController<TMessageContext, TMessageResult, TMessageMetadata, TCallbackContext, TCallbackMetadata> : Controller
    where TMessageMetadata : Attribute, IHandlerAttribute<Message, TMessageContext>
    where TCallbackMetadata : Attribute, IHandlerAttribute<CallbackQuery, TCallbackContext>
  {
    private readonly TelemetryClient myTelemetryClient;
    private readonly ITelegramBotClient myTelegramBotClient;
    private readonly IEnumerable<Meta<Func<Message, IMessageHandler<TMessageContext, TMessageResult>>, TMessageMetadata>> myMessageHandlers;
    private readonly IEnumerable<Meta<Func<Update, ICallbackQueryHandler<TCallbackContext>>, TCallbackMetadata>> myCallbackQueryHandlers;
    private readonly IEnumerable<Meta<Func<Update, IInlineQueryHandler>, InlineQueryHandlerAttribute>> myInlineQueryHandlers;
    private readonly IEnumerable<Func<Update, IChosenInlineResultHandler>> myChosenInlineResultHandlers;

    protected TelegramController(TelemetryClient telemetryClient,
      ITelegramBotClient telegramBotClient, 
      IEnumerable<Meta<Func<Message, IMessageHandler<TMessageContext, TMessageResult>>, TMessageMetadata>> messageHandlers,
      IEnumerable<Meta<Func<Update, ICallbackQueryHandler<TCallbackContext>>, TCallbackMetadata>> callbackQueryHandlers,
      IEnumerable<Meta<Func<Update, IInlineQueryHandler>, InlineQueryHandlerAttribute>> inlineQueryHandlers,
      IEnumerable<Func<Update, IChosenInlineResultHandler>> chosenInlineResultHandlers)
    {
      myTelemetryClient = telemetryClient;
      myTelegramBotClient = telegramBotClient;
      myMessageHandlers = messageHandlers;
      myCallbackQueryHandlers = callbackQueryHandlers;
      myInlineQueryHandlers = inlineQueryHandlers;
      myChosenInlineResultHandlers = chosenInlineResultHandlers;
    }

    [HttpPost("/update")]
    public async Task<IActionResult> Update([CanBeNull, FromBody] Update update, CancellationToken cancellationToken = default)
    {
      IActionResult Return(bool? result) =>
        result is bool success && success ? Ok() : Ok(); // TODO: not handled

      var operation = myTelemetryClient.StartOperation(new DependencyTelemetry(myTelegramBotClient.GetType().Namespace, Request.Host.ToString(), update?.Type.ToString(), update?.Id.ToString()));
      try
      {
        if (update == null)
        {
          foreach (var errorEntry in ModelState)
          {
            operation.Telemetry.Properties[$"ModelState.{errorEntry.Key}"] = errorEntry.Value.AttemptedValue;
            var errors = errorEntry.Value.Errors;
            for (var i = 0; i < errors.Count; i++)
            {
              operation.Telemetry.Properties[$"ModelState.{errorEntry.Key}.{i}"] = errors[i].ErrorMessage;
              if (errors[i].Exception is Exception exception)
              {
                myTelemetryClient.TrackException(exception, new Dictionary<string, string> { { errorEntry.Key, errorEntry.Value.AttemptedValue } });
              }
            }
          }
          throw new ArgumentNullException(nameof(update));
        }

        Message message = null;
        switch (update.Type)
        {
          case UpdateType.Message:
            message = update.Message;
            break;

          case UpdateType.ChannelPost:
            message = update.ChannelPost;
            break;

          case UpdateType.CallbackQuery:
            var callbackQuery = update.CallbackQuery;
            operation.Telemetry.Properties["uid"] = callbackQuery.From?.Username;
            operation.Telemetry.Properties["data"] = callbackQuery.Data;
            try
            {
              (var text, var showAlert, string url) = await HandlerExtentions<(string, bool, string)>.Handle(myCallbackQueryHandlers.Bind(update), callbackQuery, GetCallbackContext(callbackQuery), cancellationToken).ConfigureAwait(false);
              await myTelegramBotClient.AnswerCallbackQueryAsync(callbackQuery.Id, text, showAlert, url, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
              await myTelegramBotClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Operation timed out. Please, try again.", true, cancellationToken: cancellationToken);
              throw;
            }
            return Ok();

          case UpdateType.InlineQuery:
            var inlineQuery = update.InlineQuery;
            operation.Telemetry.Properties["uid"] = inlineQuery.From?.Username;
            operation.Telemetry.Properties["query"] = inlineQuery.Query;
            return Return(await HandlerExtentions<bool?>.Handle(myInlineQueryHandlers.Bind(update), inlineQuery, new object(), cancellationToken).ConfigureAwait(false));

          case UpdateType.ChosenInlineResult:
            var chosenInlineResult = update.ChosenInlineResult;
            operation.Telemetry.Properties["uid"] = chosenInlineResult.From?.Username;
            operation.Telemetry.Properties["query"] = chosenInlineResult.Query;
            operation.Telemetry.Properties["result"] = chosenInlineResult.ResultId;
            return Return(await HandlerExtentions<bool?>.Handle(myChosenInlineResultHandlers.Bind(update), chosenInlineResult, new object(), cancellationToken).ConfigureAwait(false));
        }

        if (message == null)
          return Ok();

        operation.Telemetry.Context.User.AccountId = (message.From?.Id ?? message.ForwardFrom?.Id)?.ToString();
        operation.Telemetry.Context.User.AuthenticatedUserId = message.From?.Username ?? message.ForwardFrom?.Username;
        operation.Telemetry.Properties["uid"] = message.From?.Username ?? message.ForwardFrom?.Username;
        operation.Telemetry.Properties["messageType"] = message.Type.ToString();
        operation.Telemetry.Properties["chat"] = message.Chat.Username;

        await ProcessMessage(async (msg, context, properties, ct) =>
        {
          foreach (var property in properties)
          {
            operation.Telemetry.Properties.Add(property);
          }

          return await HandlerExtentions<TMessageResult>.Handle(myMessageHandlers.Bind(message), message, context, ct).ConfigureAwait(false);
          //return operation.Telemetry.Success = ;
        }, message, cancellationToken);

        operation.Telemetry.Success = true;
        return Ok() /* TODO: not handled */;

      }
      catch (OperationCanceledException operationCanceledException) when (!cancellationToken.IsCancellationRequested)
      {
        operation.Telemetry.Success = false;
        myTelemetryClient.TrackException(new ExceptionTelemetry(operationCanceledException) { SeverityLevel = SeverityLevel.Warning });
        return Ok();
      }
      catch (Exception ex)
      {
        operation.Telemetry.Success = false;
        myTelemetryClient.TrackException(ex);
        return Ok();
      }
      finally
      {
        operation.Dispose();
      }
    }

    protected virtual TMessageContext GetMessageContext(Message message) => default;

    protected virtual TCallbackContext GetCallbackContext(CallbackQuery callbackQuery) => default;
    
    protected virtual Task<TMessageResult> ProcessMessage(Func<Message, TMessageContext, IDictionary<string, string>, CancellationToken, Task<TMessageResult>> processor,
                                                  Message message,  CancellationToken cancellationToken = default)
    {
      return processor(message, GetMessageContext(message), new Dictionary<string, string>(0), cancellationToken);
    }
  }
}