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
    private readonly TimezoneCommandHandler myTimezoneCommandHandler;

    public LocationMessageHandler(TimezoneCommandHandler timezoneCommandHandler)
    {
      myTimezoneCommandHandler = timezoneCommandHandler;
    }
    
    public async Task<bool?> Handle(Message data, (UpdateType updateType, PollMessage context) context = default, CancellationToken cancellationToken = default)
    {
      return await myTimezoneCommandHandler.ProcessLocation(data, cancellationToken);
    }
  }
}