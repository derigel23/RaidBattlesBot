using System;
using System.Collections.Generic;

namespace RaidBattlesBot.Configuration
{
  public class BotConfiguration
  {
    public string[] BotTokens { get; set; }
    public TimeSpan Timeout { get; set; }
    public TimeSpan VoteTimeout { get; set; }
    public TimeSpan NotificationLeadTime { get; set; }
    public long? LogChatId{ get; set; }
    public HashSet<int> BlackList { get; set; }
    public string[] SkipDomains { get; set; }
  }
}