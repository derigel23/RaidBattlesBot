using Telegram.Bot.Types;

namespace Team23.TelegramSkeleton
{
  public interface ICallbackQueryHandler<in TContext> : IHandler<CallbackQuery, TContext, (string text, bool showAlert, string url)>
  {
    
  }
}