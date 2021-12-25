using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Model
{
  public static class PlayerEx
  {
    [ItemCanBeNull]
    public static async Task<Player> Get(this DbSet<Player> players, [NotNull] User user, CancellationToken cancellationToken = default)
    {
      return await players.FindAsync(new object[] { user.Id }, cancellationToken);
    }
  }
}