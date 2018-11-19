using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Handlers;

namespace RaidBattlesBot.Model
{
  public static class RaidHelpers
  {
    public static (decimal lat, decimal lon) LowerPrecision(decimal lat, decimal lon, int precision, MidpointRounding rounding) =>
      (decimal.Round(lat, precision, rounding), decimal.Round(lon, precision, rounding));

    public static IOrderedQueryable<Raid> FindKnownGym(this DbSet<Raid> encounters, decimal lat, decimal lon, int? precision = null, MidpointRounding? rounding = null)
    {
      var possibleEncounters = encounters
        .Where(_ => _.Gym != null || _.PossibleGym != null)
        .Where(_ => _.Lon != null && _.Lat != null);

      if (precision is int precisionValue)
      {
        var precisionRounding = rounding ?? GymHelper.LowerDecimalPrecisionRounding;
        (lat, lon) = LowerPrecision(lat, lon, precisionValue, precisionRounding);
        possibleEncounters = possibleEncounters
          .Where(_ => decimal.Round(_.Lon.Value, precisionValue, precisionRounding) == lon)
          .Where(_ => decimal.Round(_.Lat.Value, precisionValue, precisionRounding) == lat);
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