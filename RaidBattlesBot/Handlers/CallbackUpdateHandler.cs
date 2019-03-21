using System;
using System.Collections.Generic;
using Autofac.Features.Metadata;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  public class CallbackUpdateHandler : CallbackUpdateHandler<object, CallbackQueryHandlerAttribute>
  {
    public CallbackUpdateHandler(ITelegramBotClient telegramBotClient, IEnumerable<Meta<Func<Update, ICallbackQueryHandler<object>>, CallbackQueryHandlerAttribute>> callbackQueryHandlers)
      : base(telegramBotClient, callbackQueryHandlers) { }
  }
}