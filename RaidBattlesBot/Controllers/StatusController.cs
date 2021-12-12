using System;
using System.Collections.Generic;
using RaidBattlesBot.Handlers;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Controllers
{
  public class StatusController : StatusController<PollMessage, bool?, BotCommandAttribute>
  {
    public StatusController(IWebHookSaltProvider? webHookSaltProvider, IEnumerable<ITelegramBotClient> bots, IEnumerable<IStatusProvider> statusProviders,
      IEnumerable<Lazy<Func<Message, IBotCommandHandler<PollMessage, bool?>>, BotCommandAttribute>> commandHandlers)
      : base(webHookSaltProvider, bots, statusProviders, commandHandlers)
    {
    }
  }
}