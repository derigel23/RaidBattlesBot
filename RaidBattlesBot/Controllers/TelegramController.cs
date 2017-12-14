using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Handlers;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Controllers
{
  public class TelegramController : Controller
  {
    private readonly TelemetryClient myTelemetryClient;
    private readonly IEnumerable<Meta<Func<Message, IMessageHandler>>> myMessageHandlers;

    public TelegramController(TelemetryClient telemetryClient, IEnumerable<Meta<Func<Message, IMessageHandler>>> messageHandlers)
    {
      myTelemetryClient = telemetryClient;
      myMessageHandlers = messageHandlers;
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
            break;

          case UpdateType.InlineQueryUpdate:
            break;
        }


        if (message == null)
          return Ok();

        HttpContext.Items["uid"] = (message.ForwardFrom ?? message.From)?.Username;
        HttpContext.Items["messageType"] = message.Type.ToString();
        HttpContext.Items["chat"] = message.Chat.Username;

        return await HandlerExtentions<bool>.Handle(myMessageHandlers.Bind(message), (MessageTypeAttribute attr) => attr.MessageType, m => m.Type, message, new object(), cancellationToken)
          ? Ok() : Ok() /* TODO: not handled */;

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