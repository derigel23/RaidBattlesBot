using System;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Configuration
{
  public class IngressConfiguration
  {
    public Uri ServiceUrl { get; set; }
    public Location DefaultLocation { get; set; }
    public TimeSpan Timeout { get; set; }
  }
}