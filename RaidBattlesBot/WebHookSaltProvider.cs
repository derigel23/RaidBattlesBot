using Microsoft.Extensions.Options;
using RaidBattlesBot.Configuration;
using Team23.TelegramSkeleton;

namespace RaidBattlesBot;

public class WebHookSaltProvider : IWebHookSaltProvider
{
  private readonly IOptions<BotConfiguration> myOptions;

  public WebHookSaltProvider(IOptions<BotConfiguration> options)
  {
    myOptions = options;
  }
  
  public int? GetSalt(long? botId) => myOptions.Value?.BotSalt;
}