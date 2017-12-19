using System;

namespace RaidBattlesBot.Model
{
  public interface ITrackable
  {
    DateTimeOffset? Modified { get; set; }
  }
}