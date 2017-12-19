using Telegram.Bot.Types;

namespace RaidBattlesBot.Model
{
  public static class UserEx
  {
    public static string GetLink(this User user)
    {
      return $"[{string.Join(" ", user.FirstName, user.LastName)}](tg://user?id={user.Id})";
    }
  }
}