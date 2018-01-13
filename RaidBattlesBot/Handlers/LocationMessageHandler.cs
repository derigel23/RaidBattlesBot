using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.LocationMessage)]
  public class LocationMessageHandler : IMessageHandler
  {
    private readonly RaidBattlesContext myDb;
    private readonly Gyms myGyms;

    public LocationMessageHandler(RaidBattlesContext db, Gyms gyms)
    {
      myDb = db;
      myGyms = gyms;
    }
    public async Task<bool?> Handle(Message message, PollMessage pollMessage, CancellationToken cancellationToken = default)
    {
      var location = message.Location;
      if (location == null) return null;

      if (!myGyms.TryGet((decimal) location.Latitude, (decimal) location.Longitude, out var foundGym, Gyms.LowerDecimalPrecision))
      {
        return null;
      }

      var existingRaid = await myDb.Raids
        .Where(_ => _.RaidBossEndTime > DateTimeOffset.Now)
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