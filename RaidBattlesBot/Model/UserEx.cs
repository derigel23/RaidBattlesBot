using System;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Model
{
  public static class UserEx
  {
    public static readonly Func<User, StringBuilder, ParseMode, StringBuilder> DefaultUserExtractor =
      (user, builder, mode) => builder.Sanitize(" ".JoinNonEmpty(user.FirstName, user.LastName), mode);
    
    public static StringBuilder GetLink(this User user, ParseMode parseMode = Helpers.DefaultParseMode)
    {
      return GetLink(user, DefaultUserExtractor, parseMode);
    }
    
    public static StringBuilder GetLink(this User user, Func<User, StringBuilder, ParseMode, StringBuilder> userFormatter, ParseMode parseMode = Helpers.DefaultParseMode)
    {
      var builder = new StringBuilder();
      return builder.Link((b, m) => userFormatter(user, b, m) , $"tg://user?id={user.Id}", parseMode);
    }

  }
}