using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageEntityType(EntityType = MessageEntityType.Url)]
  public class UrlMessageEntityHandler : UrlLikeMessageEntityHandler
  {
  }
}