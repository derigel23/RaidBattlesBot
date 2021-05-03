using System;

namespace RaidBattlesBot.Model
{
  public class Player : ITrackable
  {
    public long UserId { get; set; }
    public string Nickname { get; set; }
    public DateTimeOffset? Modified { get; set; }
  }
}