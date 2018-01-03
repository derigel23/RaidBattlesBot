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
            var callBackResponse = await HandlerExtentions<string>.Handle(myCallbackQueryHandlers.Bind(update), callbackQuery, new object(), cancellationToken);
            return await myTelegramBotClient.AnswerCallbackQueryAsync(callbackQuery.Id, callBackResponse, cancellationToken: cancellationToken)
              ? Ok() : Ok() /* TODO: not handled */;

          case UpdateType.InlineQueryUpdate:
            return (await HandlerExtentions<bool?>.Handle(myInlineQueryHandlers.Bind(update), update.InlineQuery, new object(), cancellationToken)).GetValueOrDefault()
              ? Ok() : Ok() /* TODO: not handled */;
          
          case UpdateType.ChosenInlineResultUpdate:
            return (await HandlerExtentions<bool?>.Handle(myChosenInlineResultHandlers.Bind(update), update.ChosenInlineResult, new object(), cancellationToken)).GetValueOrDefault()
              ? Ok() : Ok() /* TODO: not handled */;

        }


        if (message == null)
          return Ok();

        HttpContext.Items["uid"] = (message.ForwardFrom ?? message.From)?.Username;
        HttpContext.Items["messageType"] = message.Type.ToString();
        HttpContext.Items["chat"] = message.Chat.Username;

        var pollMessage = new PollMessage(message);
        if ((await HandlerExtentions<bool?>.Handle(myMessageHandlers.Bind(message), message, pollMessage, cancellationToken)).GetValueOrDefault())
        {
          await myRaidService.AddPollMessage(pollMessage, cancellationToken);
        };

        return Ok() /* TODO: not handled */;

      }
      catch (Exception ex)
      {
        myTelemetryClient.TrackException(ex, HttpContext.Properties());
        return Ok();
      }
      finally
      {
        var eventName = update.Type.ToString();
        myTelemetryClient.TrackEvent(eventName, HttpContext.Properties());
      }
    }
  }
}