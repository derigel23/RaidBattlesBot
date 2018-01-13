using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace RaidBattlesBot.Model
{
  public static class RaidHelpers
  {
    public static (decimal lat, decimal lon) LowerPrecision(decimal lat, decimal lon, int precision) =>
      (decimal.Round(lat, precision, MidpointRounding.ToEven), decimal.Round(lon, precision, MidpointRounding.ToEven));

    public static IOrderedQueryable<Raid> FindKnownGym(this DbSet<Raid> encounters, decimal lat, decimal lon, int? precision = null)
    {
      var possibleEncounters = encounters
        .Where(_ => _.Gym != null || _.PossibleGym != null)
        .Where(_ => _.Lon != null && _.Lat != null);

      if (precision is int precisionValue)
      {
        (lat, lon) = LowerPrecision(lat, lon, precisionValue);
        possibleEncounters = possibleEncounters
          .Where(_ => decimal.Round(_.Lon.Value, precisionValue, MidpointRounding.ToEven) == lon)
          .Where(_ => decimal.Round(_.Lat.Value, precisionValue, MidpointRounding.ToEven) == lat);
      }
      else
      {
        possibleEncounters = possibleEncounters
          .Where(_ => _.Lon == lon && _.Lat == lat);
      }

      return possibleEncounters
        .OrderByDescending(_ => _.Id)
        .ThenByDescending(_ => _.Gym != null);
    }
  }
}