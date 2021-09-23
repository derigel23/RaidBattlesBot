using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Geolocation;
using JetBrains.Annotations;
using NodaTime;
using NodaTime.TimeZones;

namespace RaidBattlesBot
{
  public class TimeZoneService
  {
    private readonly IDateTimeZoneProvider myDateTimeZoneProvider;
    private readonly Dictionary<string, RegionInfo> myTimeZoneRegions = new();
    private readonly Dictionary<string, List<TzdbZoneLocation>> myTimeZoneAbbreviations = new(StringComparer.OrdinalIgnoreCase);

    // some manual abbreviations
    private static ILookup<string, string> ourAbbreviations = new List<KeyValuePair<string, string>>
    {
      KeyValuePair.Create("Europe/Moscow", "МСК"),
      KeyValuePair.Create("Asia/Novosibirsk", "НСК"),
      KeyValuePair.Create("Asia/Novosibirsk", "NSK")
    }.ToLookup(pair => pair.Key, pair => pair.Value);
      
    public TimeZoneService(IClock clock, IDateTimeZoneProvider dateTimeZoneProvider)
    {
      myDateTimeZoneProvider = dateTimeZoneProvider;
      
      var instant = clock.GetCurrentInstant();
      // reverse tz map

      void AddAbbr(string abbr, TzdbZoneLocation zoneLocation)
      {
        if (!myTimeZoneAbbreviations.TryGetValue(abbr, out var zones))
        {
          myTimeZoneAbbreviations[abbr] = zones = new List<TzdbZoneLocation>();
        }

        zones.Add(zoneLocation);
      }

      foreach (var zoneLocation in TzdbDateTimeZoneSource.Default.ZoneLocations!)
      {
        var dateTimeZone = dateTimeZoneProvider[zoneLocation.ZoneId];
        try
        {
          myTimeZoneRegions[dateTimeZone.Id] = new RegionInfo(zoneLocation.CountryCode);
        }
        catch (ArgumentException)
        {
        }

        var interval = new Interval(instant.Minus(Duration.FromDays(185)), instant.Plus(Duration.FromDays(185)));
        foreach (var zoneInterval in dateTimeZone.GetZoneIntervals(interval, ZoneEqualityComparer.Options.MatchNames))
        {
          if (!zoneInterval.Name.Any(char.IsLetter)) continue; // no digital offsets 
          AddAbbr(zoneInterval.Name, zoneLocation);
        }
        
        foreach (var abbr in ourAbbreviations[dateTimeZone.Id])
        {
          AddAbbr(abbr, zoneLocation);
        }
      }
    }

    public bool TryGetRegion(string timeZoneId, out RegionInfo region) =>
      myTimeZoneRegions.TryGetValue(timeZoneId, out region);

    public bool TryGetTimeZoneByAbbreviation(string abbr, [CanBeNull] Telegram.Bot.Types.Location location, out DateTimeZone dateTimeZone)
    {
      dateTimeZone = null;
      
      // first check by full time zone id
      if (myDateTimeZoneProvider.GetZoneOrNull(abbr) is {} dtz)
      {
        dateTimeZone = dtz;
        return true;
      }
      
      // next, try find by abbreviation
      if (!myTimeZoneAbbreviations.TryGetValue(abbr, out var zoneLocations))
        return false;

      if (zoneLocations.Count == 1)
      {
        dateTimeZone = myDateTimeZoneProvider[zoneLocations[0].ZoneId];
        return true;
      }

      var coordinate = location != null ? new Coordinate(location.Latitude, location.Longitude) : new Coordinate(0, 0);
      if (zoneLocations
        .Select(zl => (zl.ZoneId, GeoCalculator.GetDistance(coordinate, new Coordinate(zl.Latitude, zl.Longitude))))
        .OrderBy(tuple => tuple.Item2)
        .FirstOrDefault() is var (zoneId, _))
      {
        dateTimeZone = myDateTimeZoneProvider[zoneId];
        return true;
      }

      return false;
    }
  }  
}