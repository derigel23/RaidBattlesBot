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
using RaidBattlesBot.Migrations;
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
    private readonly DateTimeZone myDateTimeZone;

    public VenueMessageHandler(RaidBattlesContext db, PokemonInfo pokemonInfo, GymHelper gymHelper, DateTimeZone dateTimeZone)
    {
      myDb = db;
      myPokemonInfo = pokemonInfo;
      myGymHelper = gymHelper;
      myDateTimeZone = dateTimeZone;
    }

    public async Task<bool?> Handle(Message venueMessage, PollMessage pollMessage , CancellationToken cancellationToken = default)
    {
      var venue = venueMessage.Venue;
      if (venue == null) return null;

      if (venueMessage.Chat.Type == ChatType.Channel)
        return false;

      var raid = new Raid
      {
        Lat = (decimal)venue.Location.Latitude,
        Lon = (decimal)venue.Location.Longitude,
      };
      string startTimeString = null;
      string endTimeString = null;
      switch (venue.Title)
      {
        case string title when ourUser275BossPattern.Match(title) is var pokemon275match && pokemon275match.Success:
          raid.ParseRaidInfo(myPokemonInfo, pokemon275match.Groups["name"].Value);
          raid.Move1 = pokemon275match.Groups["move1"].Value;
          raid.Move2 = pokemon275match.Groups["move2"].Value;
          if (ourUser275EndTimePattern.Match(venue.Address) is var endTimeMatch && endTimeMatch.Success)
          {
            endTimeString = endTimeMatch.Groups["end"].Value;
          }
          if (ourUser275GymPattern.Match(venue.Address) is var bossGymMatch && bossGymMatch.Success)
          {
            raid.Gym = bossGymMatch.Groups["gym"].Value;
          }
          break;
        case string title when ourUser275EggPattern.Match(title) is var egg275match && egg275match.Success:
          raid.Name = "Egg";
          raid.RaidBossLevel = int.TryParse(egg275match.Groups["level"].Value, out int raidBossLevel) ? raidBossLevel : default(int?);
          endTimeString = egg275match.Groups["end"].Value;
          if (ourUser275GymPattern.Match(venue.Address) is var eggGymMatch && eggGymMatch.Success)
          {
            raid.Gym = eggGymMatch.Groups["gym"].Value;
          }
          break;
        case string title when ourRaidPattern.Match(title) is var match && match.Success:
          raid.ParseRaidInfo(myPokemonInfo, match.Groups["name"].Value, venue.Address.Split(Environment.NewLine.ToCharArray(), 2)[0]);
          startTimeString = match.Groups["start"].Value;
          endTimeString = match.Groups["end"].Value;
          break;
        default:
          return null;
      }
 
      var messageDate = venueMessage.GetMessageDate(myDateTimeZone);
      raid.StartTime = messageDate.ParseTime(startTimeString, out var startTime) ? startTime : messageDate.ToDateTimeOffset();
      raid.RaidBossEndTime = messageDate.ParseTime(endTimeString, out var endTime) ? endTime : default(DateTimeOffset?);

      var gymInfo = await raid
        .SetTitleAndDescription(new StringBuilder(), new StringBuilder(), myGymHelper, Gyms.LowerDecimalPrecision, Gyms.LowerDecimalPrecisionRounding, cancellationToken);
      raid.Lat = gymInfo.location.lat;
      raid.Lon = gymInfo.location.lon;

      pollMessage.Poll = new Poll(venueMessage)
      {
        Raid = raid,
        Time = raid.GetDefaultPollTime()
      };

      return true;
    }

    private static readonly Regex ourRaidPattern = new Regex(@"(?<name>.+) (?<start>\d+:\d+)→(?<end>\d+:\d+)");
    
    private static readonly Regex ourUser275BossPattern = new Regex(@"(?<name>.+) \((?<move1>.+)/(?<move2>.+)\)");
    private static readonly Regex ourUser275EggPattern = new Regex(@"Egg (?<level>\d) lvl - (?<end>\d+:\d+(:\d+)?)");
    private static readonly Regex ourUser275EndTimePattern = new Regex(@"Until: (?<end>\d+:\d+(:\d+)?)");
    private static readonly Regex ourUser275GymPattern = new Regex(@"Gym: (?<gym>.+?)( \(.+?\))?\.");
  }
}