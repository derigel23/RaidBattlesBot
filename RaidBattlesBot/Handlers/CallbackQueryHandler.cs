using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  public class CallbackQueryHandler : ICallbackQueryHandler
  {
    public async Task<bool> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      return false;
    }
  }
}