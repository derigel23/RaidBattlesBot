using System;

namespace RaidBattlesBot.Model
{
  public class Notification
  {
    public int PollId { get; set; }
    public long ChatId { get; set; }
    public DateTimeOffset? DateTime { get; set; }
    public long? BotId { get; set; }
    public Poll Poll { get; set; }
  }
}