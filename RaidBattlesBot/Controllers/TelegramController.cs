using System;
using System.Collections.Generic;
using Autofac.Features.Metadata;
using Microsoft.ApplicationInsights;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Controllers
{
  public class TelegramController : Team23.TelegramSkeleton.TelegramController
  {
    public TelegramController(TelemetryClient telemetryClient, IEnumerable<Meta<Func<Update, IUpdateHandler<bool?>>, UpdateHandlerAttribute>> updateHandlers)
      : base(telemetryClient.GetType().Namespace, telemetryClient, updateHandlers) { }
  }
}