using System;
using System.Collections.Generic;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Model
{
  public class Poll : ITrackable
  {
    public Poll() {  }

    public Poll(Message message)
    {
      Owner = message.From.Id;
    }

    public int Id { get; set; }
    public int? RaidId { get; set; }
    public int? Owner { get; set; }
    public string Title { get; set; }
    public DateTimeOffset? Time { get; set; }
    public DateTimeOffset? Modified { get; set; }
    public bool Cancelled { get; set; }

    public Raid Raid { get; set; }
    public List<PollMessage> Messages { get; set; }
    public List<Vote> Votes { get; set; }
  }
}