using System;
using Microsoft.Extensions.Options;
using RaidBattlesBot.Configuration;
using Team23.TelegramSkeleton;

namespace RaidBattlesBot;

public class WebHookSaltProvider : IWebHookSaltProvider
{
  private readonly long mySalt;

  public WebHookSaltProvider(IOptions<BotConfiguration> options)
  {
    mySalt = new Random(options.Value?.BotSalt ?? 0).NextInt64();
  }
  
  public long GetSalt() => mySalt;
}