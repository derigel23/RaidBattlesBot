using System.Threading;
using System.Threading.Tasks;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.Photo)]
  public class PhotoMessageHandler : IMessageHandler<PollMessage>
  {
    public async Task<bool?> Handle(Message data, PollMessage pollMessage = default, CancellationToken cancellationToken = default)
    {
      return null;
    }
  }
}