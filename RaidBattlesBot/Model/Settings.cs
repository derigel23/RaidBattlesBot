using System;

namespace RaidBattlesBot.Model
{
  public class Settings : ITrackable
  {
    public int Id { get; set; }
    public long Chat { get; set; }
    public VoteEnum Format { get; set; }
    public int Order { get; set; }
    public DateTimeOffset? Modified { get; set; }
  }
}