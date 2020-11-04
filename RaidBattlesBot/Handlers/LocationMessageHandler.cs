using System.Threading;
using System.Threading.Tasks;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(UpdateType.Message, MessageType = MessageType.Location)]
  public class LocationMessageHandler : IMessageHandler
  {
    private readonly UserSettingsCommandHandler myUserSettingsCommandHandler;

    public LocationMessageHandler(UserSettingsCommandHandler userSettingsCommandHandler)
    {
      myUserSettingsCommandHandler = userSettingsCommandHandler;
    }
    
    public async Task<bool?> Handle(Message data, (UpdateType updateType, PollMessage context) context = default, CancellationToken cancellationToken = default)
    {
      return await myUserSettingsCommandHandler.ProcessLocation(data, cancellationToken);
    }
  }
}