using System;
using System.Collections;
using System.Collections.Generic;

namespace RaidBattlesBot.Configuration
{
  public class BotConfiguration
  {
    public string BotToken { get; set; }
    public TimeSpan Timeout { get; set; }
    public TimeSpan VoteTimeout { get; set; }
    public long? LogChatId{ get; set; }
    public HashSet<int> BlackList { get; set; }
  }
}