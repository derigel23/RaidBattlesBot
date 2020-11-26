using System;

namespace RaidBattlesBot.Model
{
  [Flags]
  public enum PollMode : byte
  {
    None = 0,
    Invitation = 1,
    Nicknames = 2,
    Names = 4,
    Usernames = 8,
    
    Default = Names
  }
}