using System;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Model
{
  public class Vote : ITrackable
  {
    public int PollId { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; }
    public string LasttName { get; set; }
    public int? Team { get; set; }
    public DateTimeOffset? Modified { get; set; }

    public User User
    {
      get => new User { Id = UserId, FirstName = FirstName, LastName = LasttName };
      set
      {
        UserId = value.Id;
        FirstName = value.FirstName;
        LasttName = value.LastName;
      }
    }
  }
}