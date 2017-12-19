using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  public abstract class UrlLikeMessageEntityHandler : IMessageEntityHandler
  {
    public async Task<bool> Handle(MessageEntity entity, object context = default , CancellationToken cancellationToken = default)
    {
      return false;
    }
  }
}