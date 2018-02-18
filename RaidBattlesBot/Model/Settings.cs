using System;

namespace RaidBattlesBot.Model
{
  public class Settings : ITrackable
  {
    public long Chat { get; set; }
    public VoteEnum DefaultAllowedVotes { get; set; }
    public DateTimeOffset? Modified { get; set; }
  }
}