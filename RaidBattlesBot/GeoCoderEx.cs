using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using Telegram.Bot.Types;

namespace RaidBattlesBot
{
  public class GeoCoderEx
  {
    private readonly DateTimeZone myDefaultDateTimeZone;
    private readonly GeoCoder myGeoCoder;
    
    public GeoCoderEx(DateTimeZone defaultDateTimeZone, GeoCoder geoCoder)
    {
      myDefaultDateTimeZone = defaultDateTimeZone;
      myGeoCoder = geoCoder;
    }
    
    public async Task<DateTimeZone> GetTimeZone(InlineQuery inlineQuery, CancellationToken cancellationToken = default)
    {
      if (inlineQuery.Location is {} userLocation)
      {
        var location = new GoogleMapsApi.Entities.Common.Location(userLocation.Latitude, userLocation.Longitude);
        return await myGeoCoder.GetTimeZone(location, cancellationToken);
      }

      return myDefaultDateTimeZone;
    }
  }
}