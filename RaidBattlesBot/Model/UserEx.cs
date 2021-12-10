using System;
using System.Runtime.CompilerServices;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Model
{
  public static class UserEx
  {

    public static Func<User, TextBuilder, TextBuilder> GetDefaultUserExtractor(Func<TextBuilder, string, TextBuilder> appender) =>
      (user, builder) => appender(builder, " ".JoinNonEmpty(user.FirstName, user.LastName) is {} name  && !string.IsNullOrWhiteSpace(name) ? name : user.Username);

    public static readonly Func<User, TextBuilder, TextBuilder> DefaultUserExtractor =
      GetDefaultUserExtractor((builder, text) => builder.Sanitize(text));

    public static readonly Func<User, TextBuilder, TextBuilder> NicknameSanitizingDefaultUserExtractor =
      GetDefaultUserExtractor((builder, text) => builder.SanitizeNickname(text));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TextBuilder GetLink(this User user, TextBuilder builder = default)
    {
      return GetLink(user, builder ?? new TextBuilder(), DefaultUserExtractor);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TextBuilder GetLink(this User user, TextBuilder builder, Func<User, TextBuilder, TextBuilder> userFormatter)
    {
      return builder.Link(b => userFormatter(user, b), user);
    }

  }
}