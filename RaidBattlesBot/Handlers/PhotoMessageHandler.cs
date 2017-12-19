using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.PhotoMessage)]
  public class PhotoMessageHandler : IMessageHandler
  {
    public async Task<bool> Handle(Message data, object context = default, CancellationToken cancellationToken = default)
    {
      return false;
    }
  }
}