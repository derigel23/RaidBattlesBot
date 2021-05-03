using System;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Model
{
  public class Vote : ITrackable
  {
    public long? BotId { get; set; }
    public int PollId { get; set; }
    public long UserId { get; set; }
    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public VoteEnum? Team { get; set; }
    public DateTimeOffset? Modified { get; set; }

    public User User
    {
      get => new User
      {
        Id = UserId,
        Username = Username,
        FirstName = FirstName,
        LastName = LastName
      };
      set
      {
        UserId = value.Id;
        Username = value.Username;
        FirstName = value.FirstName;
        LastName = value.LastName;
      }
    }
  }
}