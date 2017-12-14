using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  public class InlineQueryHandler : IInlineQueryHandler
  {
    public async Task<bool> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
     return false;
    }
  }
}