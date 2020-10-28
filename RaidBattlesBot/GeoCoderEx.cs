using System.Threading;
using System.Threading.Tasks;
using GeoTimeZone;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;

namespace RaidBattlesBot
{
  public class GeoCoderEx
  {
    private readonly DateTimeZone myDefaultDateTimeZone;
    private readonly RaidBattlesContext myDB;

    public GeoCoderEx(DateTimeZone defaultDateTimeZone, RaidBattlesContext db)
    {
      myDefaultDateTimeZone = defaultDateTimeZone;
      myDB = db;
    }
    
    public async Task<DateTimeZone> GetTimeZone(InlineQuery inlineQuery, CancellationToken cancellationToken = default)
    {
      if (inlineQuery.Location is {} userLocation)
      {
        if (TimeZoneLookup.GetTimeZone(userLocation.Latitude, userLocation.Longitude).Result is {} timeZoneId)
        {
          if (DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId) is { } timeZone)
          {
            return timeZone;
          }
        }
      }

      {
        var userSettings = await myDB.Set<UserSettings>()
          .SingleOrDefaultAsync(settings => settings.UserId == inlineQuery.From.Id, cancellationToken);
        if (userSettings?.TimeZoneId is { } timeZoneId && DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId) is { } timeZone)
        {
          return timeZone;
        }
      }
      
      return myDefaultDateTimeZone;
    }
  }
}