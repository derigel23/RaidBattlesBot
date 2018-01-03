using System.Threading;
using System.Threading.Tasks;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.LocationMessage)]
  public class LocationMessageHandler : IMessageHandler
  {
    public async Task<bool?> Handle(Message message, PollMessage pollMessage = default, CancellationToken cancellationToken = default)
    {
      return null;
    }
  }
}