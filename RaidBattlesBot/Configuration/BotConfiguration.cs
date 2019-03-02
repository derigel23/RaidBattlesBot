using System;

namespace RaidBattlesBot.Configuration
{
  public class BotConfiguration
  {
    public string BotToken { get; set; }
    public TimeSpan Timeout { get; set; }
    public TimeSpan VoteTimeout { get; set; }
    public long? LogChatId{ get; set; }
  }
}