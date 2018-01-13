using System.Linq;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Model
{
  public static class UserEx
  {
    public static string GetLink(this User user) =>
      $"[{"\x00A0".JoinNonEmpty(user.FirstName, user.LastName)}](tg://user?id={user.Id})";
  }
}