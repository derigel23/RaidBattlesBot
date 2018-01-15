using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Model
{
  public static class UserEx
  {
    public static StringBuilder GetLink(this User user, ParseMode mode = ParseMode.Default)
    {
      return new StringBuilder().Link(
        " ".JoinNonEmpty(user.FirstName, user.LastName).Sanitize(mode),
        $"tg://user?id={user.Id}", mode);
    }
  }
}