namespace RaidBattlesBot.Model;

public class VoteLimit
{
  public int PollId { get; set; }
  public VoteEnum Vote { get; set; }
  public int Limit { get; set; }
}