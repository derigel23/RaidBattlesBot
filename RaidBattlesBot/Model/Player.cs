using System;

namespace RaidBattlesBot.Model
{
  public class Player : ITrackable
  {
    public long UserId { get; set; }
    public string Nickname { get; set; }
    public long? FriendCode { get; set; }
    public bool? AutoApproveFriendship { get; set; }
    public DateTimeOffset? Modified { get; set; }
  }
}