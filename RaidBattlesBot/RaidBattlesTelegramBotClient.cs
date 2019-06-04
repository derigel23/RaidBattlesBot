using System.Net.Http;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;
using RaidBattlesBot.Configuration;
using Team23.TelegramSkeleton;

namespace RaidBattlesBot
{
  public class PoGoTelegramBotClient : TelegramBotClientEx
  {
    public PoGoTelegramBotClient(TelemetryClient telemetryClient, IOptions<BotConfiguration> options, HttpClient httpClient)
      : base(telemetryClient, options.Value.BotToken, httpClient)
    {
      Timeout = options.Value.Timeout;
    }
  }
}