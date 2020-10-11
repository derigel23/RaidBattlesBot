using System.Threading;
using System.Threading.Tasks;
using GeoTimeZone;
using NodaTime;
using Telegram.Bot.Types;

namespace RaidBattlesBot
{
  public class GeoCoderEx
  {
    private readonly DateTimeZone myDefaultDateTimeZone;
    
    public GeoCoderEx(DateTimeZone defaultDateTimeZone)
    {
      myDefaultDateTimeZone = defaultDateTimeZone;
    }
    
    public Task<DateTimeZone> GetTimeZone(InlineQuery inlineQuery, CancellationToken cancellationToken = default)
    {
      if (inlineQuery.Location is {} userLocation)
      {
        if (TimeZoneLookup.GetTimeZone(userLocation.Latitude, userLocation.Longitude).Result is {} timeZoneId)
        {
          if (DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId) is { } timeZone)
          {
            return Task.FromResult(timeZone);
          }
        }
      }

      return Task.FromResult(myDefaultDateTimeZone);
    }
  }
}