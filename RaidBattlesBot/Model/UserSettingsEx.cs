using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Model
{
  public static class UserSettingsEx
  {
    [ItemCanBeNull]
    public static async Task<UserSettings> Get(this DbSet<UserSettings> settings, User user, CancellationToken cancellationToken)
    {
      return await settings.FindAsync(new object[] { user.Id }, cancellationToken);
    }

    [ItemCanBeNull]
    public static async Task<Location> GetLocation(this DbSet<UserSettings> settings, InlineQuery inlineQuery, CancellationToken cancellationToken = default)
    {
      if (inlineQuery.Location is {} location)
        return location;

      var userSettings = await settings.Get(inlineQuery.From, cancellationToken);
      if (userSettings is { Lat: { } lat, Lon: { } lon})
      {
        return new Location { Latitude = (float) lat, Longitude = (float) lon };
      }

      return null;
    }
  }
}