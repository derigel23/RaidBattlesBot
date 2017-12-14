using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageEntityType(MessageEntityType.Url)]
  public class UrlMessageEntityHandler : UrlLikeMessageEntityHandler
  {
  }
}