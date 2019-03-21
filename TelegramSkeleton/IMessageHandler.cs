using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  public interface IMessageHandler<TContext, TResult> : IHandler<Message, (UpdateType updateType, TContext context), TResult>
  {
  }
}