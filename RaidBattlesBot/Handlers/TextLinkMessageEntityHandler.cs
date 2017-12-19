using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageEntityType(EntityType = MessageEntityType.TextLink)]
  public class TextLinkMessageEntityHandler : UrlLikeMessageEntityHandler
  {
  }
}