using System.Net.Http;
using Microsoft.Extensions.Options;
using RaidBattlesBot.Configuration;
using Telegram.Bot;

namespace RaidBattlesBot
{
  public class PoGoTelegramBotClient : TelegramBotClient
  {
    public PoGoTelegramBotClient(IOptions<BotConfiguration> options, HttpClient httpClient)
      : base(options.Value.BotToken, httpClient)
    {
      Timeout = options.Value.Timeout;
    }
  }
}