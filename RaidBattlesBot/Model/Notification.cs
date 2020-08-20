using System;

namespace RaidBattlesBot.Model
{
  public class Notification : ITrackable
  {
    public int UserId { get; set; }
    public NotificationType Type { get; set; }
    public DateTimeOffset? Modified { get; set; }
  }
}