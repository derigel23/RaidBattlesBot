using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.Location)]
  public class LocationMessageHandler : IMessageHandler
  {
    private readonly RaidBattlesContext myDb;
    private readonly Gyms myGyms;
    private readonly IClock myClock;

    public LocationMessageHandler(RaidBattlesContext db, Gyms gyms, IClock clock)
    {
      myDb = db;
      myGyms = gyms;
      myClock = clock;
    }
    public async Task<bool?> Handle(Message message, PollMessage pollMessage, CancellationToken cancellationToken = default)
    {
      var location = message.Location;
      if (location == null) return null;

      if (message.Chat.Type == ChatType.Channel)
        return false;

      if (!myGyms.TryGet((decimal) location.Latitude, (decimal) location.Longitude, out var foundGym, Gyms.LowerDecimalPrecision))
      {
        return null;
      }

      var now = myClock.GetCurrentInstant().ToDateTimeOffset();
      var existingRaid = await myDb.Raids
        .Where(_ => _.RaidBossEndTime > now)
        .Where(_ => _.Lat == foundGym.location.lat && _.Lon == foundGym.location.lon)
        .IncludeRelatedData()
        .FirstOrDefaultAsync(cancellationToken);

      if (existingRaid != null)
      {
        pollMessage.Poll = existingRaid.Polls.FirstOrDefault();
      }

      return true;
    }
  }
}