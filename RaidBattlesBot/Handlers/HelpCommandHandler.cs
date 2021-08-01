using System.Threading;
using System.Threading.Tasks;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [BotBotCommand(COMMAND, "Show help", Order = 51)]
  public class HelpCommandHandler : IBotCommandHandler
  {
    private const string COMMAND = "help";
    
    private readonly ITelegramBotClient myTelegramBotClient;

    public HelpCommandHandler(Message message, ITelegramBotClient telegramBotClient)
    {
      myTelegramBotClient = telegramBotClient;
    }
    
    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage context = default, CancellationToken cancellationToken = default)
    {
      switch (entity.Command.ToString().ToLowerInvariant())
      {
        case "/" + COMMAND:
          await myTelegramBotClient.SendTextMessageAsync(entity.Message.Chat, "https://telegra.ph/Kak-polzovatsya-Raid-Battles-Bot-07-31", cancellationToken: cancellationToken);
          
          return false; // processed, but not pollMessage
      }

      return null;
    }
  }
}