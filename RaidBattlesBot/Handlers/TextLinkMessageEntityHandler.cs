using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageEntityType(MessageEntityType.TextLink)]
  public class TextLinkMessageEntityHandler : UrlLikeMessageEntityHandler
  {
  }
}