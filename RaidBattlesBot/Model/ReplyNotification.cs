using System;

namespace RaidBattlesBot.Model
{
  public class ReplyNotification : ITrackable
  {
    public int BotId { get; set; }
    public int PollId { get; set; }
    public long ChatId { get; set; }
    public int MessageId { get; set; }
    public long FromChatId { get; set; }
    public int FromMessageId { get; set; }
    public DateTimeOffset? Modified { get; set; }
    
    public Poll Poll { get; set; }
  }
}