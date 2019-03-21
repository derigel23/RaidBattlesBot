using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  [UpdateHandler(UpdateType = UpdateType.CallbackQuery)]
  public abstract class CallbackUpdateHandler<TContext, TMetadata> : IUpdateHandler<bool?>
    where TMetadata : Attribute, IHandlerAttribute<CallbackQuery, TContext>
  {
    private readonly ITelegramBotClient myTelegramBotClient;
    private readonly IEnumerable<Meta<Func<Update, ICallbackQueryHandler<TContext>>, TMetadata>> myCallbackQueryHandlers;

    protected CallbackUpdateHandler(ITelegramBotClient telegramBotClient, IEnumerable<Meta<Func<Update, ICallbackQueryHandler<TContext>>, TMetadata>> callbackQueryHandlers)
    {
      myTelegramBotClient = telegramBotClient;
      myCallbackQueryHandlers = callbackQueryHandlers;
    }
    
    public async Task<bool?> Handle(Update update, OperationTelemetry telemetry, CancellationToken cancellationToken = default)
    {
      var callbackQuery = update.CallbackQuery;
      telemetry.Properties["uid"] = callbackQuery.From?.Username;
      telemetry.Properties["data"] = callbackQuery.Data;
      try
      {
        (var text, var showAlert, string url) = await HandlerExtentions<(string, bool, string)>.Handle(myCallbackQueryHandlers.Bind(update), callbackQuery, GetContext(update), cancellationToken).ConfigureAwait(false);
        await myTelegramBotClient.AnswerCallbackQueryAsync(callbackQuery.Id, text, showAlert, url, cancellationToken: cancellationToken);
      }
      catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
      {
        await myTelegramBotClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Operation timed out. Please, try again.", true, cancellationToken: cancellationToken);
        throw;
      }

      return true;
    }

    public TContext GetContext(Update update) => default;
  }
}