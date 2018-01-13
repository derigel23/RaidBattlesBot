using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using PokeTrackDecoder.Handlers;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.VenueMessage)]
  public class VenueMessageHandler : IMessageHandler
  {
    private readonly RaidBattlesContext myDb;
    private readonly PokemonInfo myPokemonInfo;
    private readonly GymHelper myGymHelper;
    private readonly Message myMessage;
    private readonly DateTimeZone myDateTimeZone;

    public VenueMessageHandler(RaidBattlesContext db, PokemonInfo pokemonInfo, GymHelper gymHelper, Message message, DateTimeZone dateTimeZone)
    {
      myDb = db;
      myPokemonInfo = pokemonInfo;
      myGymHelper = gymHelper;
      myMessage = message;
      myDateTimeZone = dateTimeZone;
    }

    public async Task<bool?> Handle(Message venueMessage, PollMessage pollMessage , CancellationToken cancellationToken = default)
    {
      var venue = venueMessage.Venue;
      if (venue == null) return null;

      var match = ourRaidPattern.Match(venue.Title);
      if (!match.Success) return null;

      var messageDate = myMessage.GetMessageDate(myDateTimeZone);

      var raid = new Raid
      {
        Lat = (decimal)venue.Location.Latitude,
        Lon = (decimal)venue.Location.Longitude,
      };

      raid.ParseRaidInfo(myPokemonInfo, match.Groups["name"].Value, venue.Address.Split(Environment.NewLine.ToCharArray(), 2)[0]);

      raid.StartTime = messageDate.ParseTime(match.Groups["start"].Value, out var startTime) ? startTime : messageDate.ToDateTimeOffset();
      raid.RaidBossEndTime = messageDate.ParseTime(match.Groups["end"].Value, out var endTime) ? endTime : default(DateTimeOffset?);

      var gymInfo = await raid
        .SetTitleAndDescription(new StringBuilder(), new StringBuilder(), myGymHelper, Gyms.LowerDecimalPrecision, Gyms.LowerDecimalPrecisionRounding, cancellationToken);
      raid.Lat = gymInfo.location.lat;
      raid.Lon = gymInfo.location.lon;

      pollMessage.Poll = new Poll(myMessage)
      {
        Raid = raid,
        Time = raid.GetDefaultPollTime()
      };

      var existingRaid = await myDb.Raids
        .Where(_ => _.Lat == raid.Lat && _.Lon == raid.Lon)
        .Where(_ => _.RaidBossEndTime == raid.RaidBossEndTime)
        .IncludeRelatedData()
        .FirstOrDefaultAsync(cancellationToken);

      if (existingRaid != null)
      {
        pollMessage.Poll = existingRaid.Polls.FirstOrDefault();
        return true;
      }

      return true;
    }

    private static readonly Regex ourRaidPattern = new Regex(@"(?<name>.+) (?<start>\d+:\d+)→(?<end>\d+:\d+)");
  }
}