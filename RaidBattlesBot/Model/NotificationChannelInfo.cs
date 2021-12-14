using System;

#nullable enable
namespace RaidBattlesBot.Model;

public class NotificationChannelInfo
{
  public string[]? Tags { get; set; }
  public TimeSpan? ActiveCheck { get; set; }
}