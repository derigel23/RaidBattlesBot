using System.Threading;
using System.Threading.Tasks;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;

namespace RaidBattlesBot.Handlers
{
  [BotBotCommand("help", "Show help", Order = 51)]
  public class HelpCommandHandler : IBotCommandHandler
  {
    private readonly ITelegramBotClient myTelegramBotClient;

    public HelpCommandHandler(ITelegramBotClient telegramBotClient)
    {
      myTelegramBotClient = telegramBotClient;
    }
    
    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage context = default, CancellationToken cancellationToken = default)
    {
      if (!this.ShouldProcess(entity, context)) return null;

      await myTelegramBotClient.SendTextMessageAsync(entity.Message.Chat, "https://telegra.ph/Kak-polzovatsya-Raid-Battles-Bot-07-31", cancellationToken: cancellationToken);
          
      return false; // processed, but not pollMessage
    }
  }
}