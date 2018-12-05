using System;
using JetBrains.Annotations;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  public interface IMessageHandler<in TContext> : IHandler<Message, TContext, bool?>
  {
  }
}