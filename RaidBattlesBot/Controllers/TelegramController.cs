using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Handlers;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Controllers
{
  public class TelegramController : Controller
  {
    private readonly TelemetryClient myTelemetryClient;
    private readonly ITelegramBotClient myTelegramBotClient;
    private readonly RaidService myRaidService;
    private readonly IEnumerable<Meta<Func<Message, IMessageHandler>, MessageTypeAttribute>> myMessageHandlers;
    private readonly IEnumerable<Meta<Func<Update, ICallbackQueryHandler>, CallbackQueryHandlerAttribute>> myCallbackQueryHandlers;
    private readonly IEnumerable<Meta<Func<Update, IInlineQueryHandler>, InlineQueryHandlerAttribute>> myInlineQueryHandlers;
    private readonly IEnumerable<Func<Update, IChosenInlineResultHandler>> myChosenInlineResultHandlers;

    public TelegramController(TelemetryClient telemetryClient,
      ITelegramBotClient telegramBotClient, RaidService raidService,
      IEnumerable<Meta<Func<Message, IMessageHandler>, MessageTypeAttribute>> messageHandlers,
      IEnumerable<Meta<Func<Update, ICallbackQueryHandler>, CallbackQueryHandlerAttribute>> callbackQueryHandlers,
      IEnumerable<Meta<Func<Update, IInlineQueryHandler>, InlineQueryHandlerAttribute>> inlineQueryHandlers,
      IEnumerable<Func<Update, IChosenInlineResultHandler>> chosenInlineResultHandlers)
    {
      myTelemetryClient = telemetryClient;
      myTelegramBotClient = telegramBotClient;
      myRaidService = raidService;
      myMessageHandlers = messageHandlers;
      myCallbackQueryHandlers = callbackQueryHandlers;
      myInlineQueryHandlers = inlineQueryHandlers;
      myChosenInlineResultHandlers = chosenInlineResultHandlers;
    }

    [HttpPost("/update")]
    public async Task<IActionResult> Update([FromBody] Update update, CancellationToken cancellationToken = default)
    {
      IActionResult Return(bool? result) =>
        result is bool success && success ? Ok() : Ok(); // TODO: not handled

      try
      {
        Message message = null;
        switch (update.Type)
        {
          case UpdateType.MessageUpdate:
            message = update.Message;
            break;

          case UpdateType.ChannelPost:
            message = update.ChannelPost;
            break;

          case UpdateType.CallbackQueryUpdate:
            var callbackQuery = update.CallbackQuery;
            HttpContext.Items["uid"] = callbackQuery.From?.Username;
            HttpContext.Items["data"] = callbackQuery.Data;
            (var text, var showAlert, string url) = await HandlerExtentions<(string, bool, string)>.Handle(myCallbackQueryHandlers.Bind(update), callbackQuery, new object(), cancellationToken);
            return Return(await myTelegramBotClient.AnswerCallbackQueryAsync(callbackQuery.Id, text, showAlert, url, cancellationToken: cancellationToken));

          case UpdateType.InlineQueryUpdate:
            var inlineQuery = update.InlineQuery;
            HttpContext.Items["uid"] = inlineQuery.From?.Username;
            HttpContext.Items["query"] = inlineQuery.Query;
            return Return(await HandlerExtentions<bool?>.Handle(myInlineQueryHandlers.Bind(update), inlineQuery, new object(), cancellationToken));

          case UpdateType.ChosenInlineResultUpdate:
            var chosenInlineResult = update.ChosenInlineResult;
            HttpContext.Items["uid"] = chosenInlineResult.From?.Username;
            HttpContext.Items["query"] = chosenInlineResult.Query;
            HttpContext.Items["result"] = chosenInlineResult.ResultId;
            return Return(await HandlerExtentions<bool?>.Handle(myChosenInlineResultHandlers.Bind(update), chosenInlineResult, new object(), cancellationToken));
        }

        if (message == null)
          return Ok();

        myTelemetryClient.Context.User.AccountId = (message.From?.Id ?? message.ForwardFrom?.Id)?.ToString();
        myTelemetryClient.Context.Properties["uid"] = myTelemetryClient.Context.User.AuthenticatedUserId =
          message.From?.Username ?? message.ForwardFrom?.Username;
        myTelemetryClient.Context.Properties["messageType"] = message.Type.ToString();
        myTelemetryClient.Context.Properties["chat"] = message.Chat.Username;

        var pollMessage = new PollMessage(message);
        if ((await HandlerExtentions<bool?>.Handle(myMessageHandlers.Bind(message), message, pollMessage,
              cancellationToken)) is bool success && success)
        {
          await myRaidService.AddPollMessage(pollMessage, Url, cancellationToken);
        }
        myTelemetryClient.Context.Properties["pollId"] = pollMessage.GetPollId()?.ToString();
        myTelemetryClient.Context.Properties["raidId"] = pollMessage.Poll?.GetRaidId()?.ToString();

        return Ok() /* TODO: not handled */;

      }
      catch (Exception ex)
      {
        myTelemetryClient.TrackException(ex);
        return Ok();
      }
      finally
      {
        var eventName = update.Type.ToString();
        myTelemetryClient.TrackEvent(eventName);
      }
    }
  }
}