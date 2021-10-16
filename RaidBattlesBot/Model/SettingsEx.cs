using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RaidBattlesBot.Model
{
  public static class SettingsEx
  {
    public static IQueryable<Settings> GetSettings(this DbSet<Settings> settings, long? chatId)
    {
      return settings
        .Where(_ => _.Chat == chatId)
        .OrderBy(_ => _.Order);
    }

    public static IQueryable<VoteEnum> GetFormats(this DbSet<Settings> settings, long? chatId)
    {
      return settings
        .GetSettings(chatId)
        .Select(_ => _.Format);
    }

    public static async Task<VoteEnum> GetFormat(this DbSet<Settings> settings, long? chatId, CancellationToken cancellationToken = default)
    {
      return await settings.GetFormats(chatId).Cast<VoteEnum?>().FirstOrDefaultAsync(cancellationToken) ?? VoteEnum.Standard;
    }
  }
}