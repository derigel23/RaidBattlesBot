using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Model
{
  public static class UserSettingsEx
  {
    public static async Task<Location> GetLocation(this DbSet<UserSettings> settings, InlineQuery inlineQuery, CancellationToken cancellationToken = default)
    {
      if (inlineQuery.Location is {} location)
        return location;

      var userSettings = await settings.Where(_ => _.UserId == inlineQuery.From.Id).SingleOrDefaultAsync(cancellationToken);
      if (userSettings is { Lat: { } lat, Lon: { } lon})
      {
        return new Location { Latitude = (float) lat, Longitude = (float) lon };
      }

      return null;
    }
  }
}