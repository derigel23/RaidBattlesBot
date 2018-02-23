using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Model
{
  public static class UserEx
  {
    public static async Task<StringBuilder> GetLink(this User user, UserInfo userInfo, ParseMode mode = ParseMode.Default, CancellationToken cancellationToken = default)
    {
      return new StringBuilder().Link(
        " ".JoinNonEmpty(user.FirstName, user.LastName).Sanitize(mode), $"tg://user?id={user.Id}",
        await userInfo.IsUserAllowed(user.Id, cancellationToken) ? mode : ParseMode.Default);
    }
  }
}