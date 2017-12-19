using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageEntityType(EntityType = MessageEntityType.BotCommand)]
  public class BotCommandMessageEntityHandler : IMessageEntityHandler
  {
    private readonly RaidService myRaidService;
    private readonly Message myMessage;

    public BotCommandMessageEntityHandler(RaidService raidService, Message message)
    {
      myRaidService = raidService;
      myMessage = message;
    }

    public async Task<bool> Handle(MessageEntity entity, object context = default, CancellationToken cancellationToken = default)
    {
      var command = myMessage.Text.Substring(entity.Offset, entity.Length);
      switch (command)
      {
        case "/raid":
          return await myRaidService.AddRaid(myMessage.Text.Substring(entity.Offset + entity.Length).Trim(), new PollMessage(myMessage), cancellationToken);
      }

      return false;
    }
  }
}