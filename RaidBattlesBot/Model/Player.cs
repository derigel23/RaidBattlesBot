using System;

namespace RaidBattlesBot.Model
{
  public class Player : ITrackable
  {
    public int UserId { get; set; }
    public string Nickname { get; set; }
    public DateTimeOffset? Modified { get; set; }
  }
}