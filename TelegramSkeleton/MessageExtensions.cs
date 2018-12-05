using NodaTime;
using Telegram.Bot.Types;

namespace Team23.TelegramSkeleton
{
  public static class MessageExtensions
  {
    public static ZonedDateTime GetMessageDate(this Message message, DateTimeZone targetZone) =>
      DateTimeZone.Utc
        .AtLeniently(LocalDateTime.FromDateTime((message.ForwardDate ?? message.Date).ToUniversalTime()))
        .WithZone(targetZone);
  }
}