using System.Threading;
using System.Threading.Tasks;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.VenueMessage)]
  public class VenueMessageHandler : IMessageHandler
  {
    public async Task<bool?> Handle(Message venueMessage, PollMessage pollMessage = default , CancellationToken cancellationToken = default)
    {
      return null;
    }
  }
}