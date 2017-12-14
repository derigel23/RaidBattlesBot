using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  public interface ICallbackQueryHandler : IHandler<CallbackQuery, object, bool>
  {
    
  }
}