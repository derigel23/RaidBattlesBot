using System;

namespace RaidBattlesBot.Model
{
  public class Friendship : ITrackable
  {
    public long Id { get; set; }
    public long FriendId { get; set; }
    public FriendshipType Type { get; set; }
    public int? PollId { get; set; }
    public DateTimeOffset? Modified { get; set; }
  }

  [Flags]
  public enum FriendshipType
  {
    Awaiting = 0,
    Approved = 1,
    Denied = 2,
  }
}