using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.LocationMessage)]
  public class LocationMessageHandler : IMessageHandler
  {
    public async Task<bool> Handle(Message message, object context = default, CancellationToken cancellationToken = default)
    {
      return false;
    }
  }
}