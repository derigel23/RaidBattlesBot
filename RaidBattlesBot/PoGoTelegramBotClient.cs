using System.Net.Http;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;
using RaidBattlesBot.Configuration;
using Team23.TelegramSkeleton;

namespace RaidBattlesBot
{
  public class PoGoTelegramBotClient : TelegramBotClientEx
  {
    public PoGoTelegramBotClient(TelemetryClient telemetryClient, string token, IOptions<BotConfiguration> options, HttpClient httpClient)
      : base(telemetryClient, token, httpClient)
    {
      Timeout = options.Value.Timeout;
    }
  }
}