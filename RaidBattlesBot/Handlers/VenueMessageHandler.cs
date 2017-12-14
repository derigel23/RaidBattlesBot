using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType.VenueMessage)]
  public class VenueMessageHandler : IMessageHandler
  {
    public async Task<bool> Handle(Message venueMessage, object context = default , CancellationToken cancellationToken = default)
    {
      return false;
    }
  }
}