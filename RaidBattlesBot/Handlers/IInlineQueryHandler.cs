using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  public interface IInlineQueryHandler : IHandler<InlineQuery, object, bool>
  {
    
  }
}