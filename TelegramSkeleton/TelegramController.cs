using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  public abstract class TelegramController<TContext> : Controller
  {
    private readonly TelemetryClient myTelemetryClient;
    private readonly ITelegramBotClient myTelegramBotClient;
    private readonly IEnumerable<Meta<Func<Message, IGenericMessageHandler<TContext>>, MessageTypeAttribute>> myMessageHandlers;
    private readonly IEnumerable<Meta<Func<Update, ICallbackQueryHandler>, CallbackQueryHandlerAttribute>> myCallbackQueryHandlers;
    private readonly IEnumerable<Meta<Func<Update, IInlineQueryHandler>, InlineQueryHandlerAttribute>> myInlineQueryHandlers;
    private readonly IEnumerable<Func<Update, IChosenInlineResultHandler>> myChosenInlineResultHandlers;

    protected TelegramController(TelemetryClient telemetryClient,
      ITelegramBotClient telegramBotClient, 
      IEnumerable<Meta<Func<Message, IGenericMessageHandler<TContext>>,MessageTypeAttribute>> messageHandlers,
      IEnumerable<Meta<Func<Update, ICallbackQueryHandler>, CallbackQueryHandlerAttribute>> callbackQueryHandlers,
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
              (var text, var showAlert, string url) = await HandlerExtentions<(string, bool, string)>.Handle(myCallbackQueryHandlers.Bind(update), callbackQuery, new object(), cancellationToken);
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
            return Return(await HandlerExtentions<bool?>.Handle(myInlineQueryHandlers.Bind(update), inlineQuery, new object(), cancellationToken));

          case UpdateType.ChosenInlineResult:
            var chosenInlineResult = update.ChosenInlineResult;
            operation.Telemetry.Properties["uid"] = chosenInlineResult.From?.Username;
            operation.Telemetry.Properties["query"] = chosenInlineResult.Query;
            operation.Telemetry.Properties["result"] = chosenInlineResult.ResultId;
            return Return(await HandlerExtentions<bool?>.Handle(myChosenInlineResultHandlers.Bind(update), chosenInlineResult, new object(), cancellationToken));
        }

        if (message == null)
          return Ok();

        operation.Telemetry.Context.User.AccountId = (message.From?.Id ?? message.ForwardFrom?.Id)?.ToString();
        operation.Telemetry.Context.User.AuthenticatedUserId = message.From?.Username ?? message.ForwardFrom?.Username;
        operation.Telemetry.Properties["uid"] = message.From?.Username ?? message.ForwardFrom?.Username;
        operation.Telemetry.Properties["messageType"] = message.Type.ToString();
        operation.Telemetry.Properties["chat"] = message.Chat.Username;

        operation.Telemetry.Success = await ProcessMessage(async (msg, context, properties, ct) =>
        {
          foreach (var property in properties)
          {
            operation.Telemetry.Properties.Add(property);
          }

          return operation.Telemetry.Success = await HandlerExtentions<bool?>.Handle(myMessageHandlers.Bind(message), message, context, ct);
        }, message, cancellationToken);
        
        return Ok() /* TODO: not handled */;

      }
      catch (OperationCanceledException operationCanceledException) when (!cancellationToken.IsCancellationRequested)
      {
        myTelemetryClient.TrackException(new ExceptionTelemetry(operationCanceledException) { SeverityLevel = SeverityLevel.Warning });
        return Ok();
      }
      catch (Exception ex)
      {
        myTelemetryClient.TrackException(ex);
        return Ok();
      }
      finally
      {
        operation.Dispose();
      }
    }

    protected virtual Task<bool?> ProcessMessage(Func<Message, TContext, IDictionary<string, string>, CancellationToken, Task<bool?>> processor,
                                                  Message message,  CancellationToken cancellationToken = default)
    {
      return processor(message, default, new Dictionary<string, string>(0), cancellationToken);
    }
  }
}