using System.Threading;
using System.Threading.Tasks;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.Photo)]
  public class PhotoMessageHandler : IMessageHandler
  {
    public Task<bool?> Handle(Message data, (UpdateType updateType, PollMessage context) _, CancellationToken cancellationToken = default)
    {
      return null;
    }
  }
}