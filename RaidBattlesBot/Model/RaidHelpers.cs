using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace RaidBattlesBot.Model
{
  public static class RaidHelpers
  {
    public static (decimal lat, decimal lon) LowerPrecision(decimal lat, decimal lon, int precision) =>
      (decimal.Round(lat, precision, MidpointRounding.AwayFromZero), decimal.Round(lon, precision, MidpointRounding.AwayFromZero));

    public static IOrderedQueryable<Raid> FindKnownGym(this DbSet<Raid> encounters, decimal lat, decimal lon, int? precision = null)
    {
      var possibleEncounters = encounters
        .Where(_ => _.Gym != null || _.PossibleGym != null)
        .Where(_ => _.Lon != null && _.Lat != null);

      if (precision.HasValue)
      {
        (lat, lon) = LowerPrecision(lat, lon, precision.Value);
        possibleEncounters = possibleEncounters
          .Where(_ => decimal.Round(_.Lon.Value, precision.Value, MidpointRounding.AwayFromZero) == lon)
          .Where(_ => decimal.Round(_.Lat.Value, precision.Value, MidpointRounding.AwayFromZero) == lat);
      }
      else
      {
        possibleEncounters = possibleEncounters
          .Where(_ => _.Lon.Value == lon && _.Lat.Value == lat);
      }

      return possibleEncounters
        .OrderByDescending(_ => _.Id)
        .ThenByDescending(_ => _.Gym != null);
    }
  }
}