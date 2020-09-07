using System;

namespace RaidBattlesBot.Model
{
  [AttributeUsage(AttributeTargets.Field)]
  public class PollModeAttribute : Attribute
  {
    public PollMode PollMode { get; }

    public PollModeAttribute(PollMode pollMode)
    {
      PollMode = pollMode;
    } 
  }
}