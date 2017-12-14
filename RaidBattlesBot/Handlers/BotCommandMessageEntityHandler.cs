using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageEntityType(MessageEntityType.BotCommand)]
  public class BotCommandMessageEntityHandler : IMessageEntityHandler
  {

    public async Task<Message> Handle(MessageEntity entity, object context = default, CancellationToken cancellationToken = default)
    {
      return null;
    }
  }
}