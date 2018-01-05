using System.Linq;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Model
{
  public static class UserEx
  {
    public static string GetLink(this User user)
    {
      return $"[{string.Join(" ", new [] { user.FirstName, user.LastName }.Where(_ => !string.IsNullOrEmpty(_)))}](tg://user?id={user.Id})";
    }
  }
}