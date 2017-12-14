using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Controllers
{
  public class TelegramController : Controller
  {
    private readonly TelemetryClient myTelemetryClient;

    public TelegramController(TelemetryClient telemetryClient)
    {
      myTelemetryClient = telemetryClient;
    }

    [HttpPost("/update")]
    public async Task<IActionResult> Update([FromBody] Update update, CancellationToken cancellationToken)
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

        return Ok();
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