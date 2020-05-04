using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using EnumsNET;

namespace RaidBattlesBot.Model
{
  [Flags]
  public enum VoteEnum : int
  {
    None = 0,

    [Display(Name = "✔", Order = 10)]
    Yes = 1 << 0,

    [Display(Name = "❤", Order = 10, Description = "You've voted as a Valor")]
    Valor = Yes << 1,

    [Display(Name = "💛", Order = 10, Description = "You've voted as an Instinct")]
    Instinct = Valor << 1,

    [Display(Name = "💙", Order = 10, Description = "You've voted as a Mystic")]
    Mystic = Instinct << 1,

    [Display(Name = "⁺¹", Order = 15)]
    Plus1 = Mystic << 1,

    [Display(Name = "+2", Order = 15)]
    Plus2 = Plus1 << 1,

    [Display(Name = "+4", Order = 15)]
    Plus4 = Plus2 << 1,

    [Display(Name = "+8", Order = 15)]
    Plus8 = Plus4 << 1,

    [Display(Name = "💤", Order = 20, Description = "You've not decided yet...")]
    MayBe = Plus8 << 1,

    [Display(Name = "✖", Order = 100, Description = "You've bailed!")]
    Cancel = MayBe << 1,

    [Display(Name = "🌐", Order = 9999)]
    Share = Cancel << 1,

    [Display(Name = "👍", Order = 10)]
    ThumbsUp = Share << 1,
    
    [Display(Name = "👎", Order = 10)]
    ThumbsDown = ThumbsUp << 1,
    
    Thumbs = ThumbsUp | ThumbsDown,

    [Display(Name = "🥊", Order = 10, Description = "You've voted as an Auror")]
    Auror = ThumbsDown << 1,
    [Display(Name = "🦎", Order = 10, Description = "You've voted as a Magizoologist")]
    Magizoologist = Auror << 1,
    [Display(Name = "🧙‍♂", Order = 10, Description = "You've voted as a Professor")]
    Professor = Magizoologist << 1,
    
    [Display(Name = "📡", Order = 10, Description = "You're going to participate remotely")]
    Remotely = Professor << 1,
    
    #region Plused votes

    [Display(Name = "❤⁺¹", Order = 1)]
    ValorPlusOne = Valor | Plus1,
    [Display(Name = "💛⁺¹", Order = 1)]
    InstinctPlusOne = Instinct | Plus1,
    [Display(Name = "💙⁺¹", Order = 1)]
    MysticPlusOne = Mystic | Plus1,

    [Display(Name = "✔⁺¹", Order = 1)]
    YesPlus1 = Yes | Plus1,

    [Display(Name = "👍⁺¹", Order = 1)]
    ThumbsUpPlus1 = ThumbsUp | Plus1,
    [Display(Name = "👎⁺¹", Order = 1)]
    ThumbsDownPlus1 = ThumbsDown | Plus1,

    [Display(Name = "🥊⁺¹", Order = 1)]
    AurorPlusOne = Auror | Plus1,
    [Display(Name = "🦎⁺¹", Order = 1)]
    MagizoologistPlusOne = Magizoologist | Plus1,
    [Display(Name = "🧙‍♂⁺¹", Order = 1)]
    ProfessorPlusOne = Professor | Plus1,

    #endregion

    Standard = ValorPlusOne | InstinctPlusOne | MysticPlusOne | Remotely | MayBe | Cancel | Share,

    Team = Valor | Instinct | Mystic,
    HarryPotter = Auror | Magizoologist | Professor,
    Going = Yes | Team | HarryPotter,
    Thinking = MayBe,
    Countable = Going | Thumbs,
    Some = Countable | MayBe ,
    SomePlus = Some | Plus,
    ChangedMind = Cancel,

    Plus = Plus1 | Plus2 | Plus4 | Plus8,
    
    Modifiers = Plus | Remotely | Share,
    Toggle = Remotely
  }

  public static class VoteEnumEx
  {
    public static readonly ICollection<VoteEnum> DefaultVoteFormats = new []
    {
      // hearts
      VoteEnum.Standard,
      
      // compact
      VoteEnum.YesPlus1 | VoteEnum.MayBe | VoteEnum.MayBe | VoteEnum.Cancel | VoteEnum.Share,
      
      // thumbs up/down
      VoteEnum.Thumbs | VoteEnum.Share
    };

    private static readonly int FirstPlusBit = (int)Math.Log((int)VoteEnum.Plus1, 2);

    public static int GetPlusVotesCount(this VoteEnum? vote) =>
      ((int)(vote?.CommonFlags(VoteEnum.Plus) ?? VoteEnum.None) >> FirstPlusBit);

    // TODO: check for overflow
    public static VoteEnum IncreaseVotesCount(this VoteEnum vote, int diff) => vote
      .RemoveFlags(VoteEnum.Plus)
      .CombineFlags((VoteEnum) ((((int)(vote.CommonFlags(VoteEnum.Plus)) >> FirstPlusBit) + diff) << FirstPlusBit));

    public static string Description(this VoteEnum vote) =>
      (vote.RemoveFlags(VoteEnum.Modifiers) is { } voteWithoutModifiers && voteWithoutModifiers.HasAnyFlags() ?
        voteWithoutModifiers : vote.HasAnyFlags(VoteEnum.Modifiers) ? VoteEnum.Yes : vote).AsString(EnumFormat.DisplayName);

    public static IEnumerable<VoteEnum> GetFlags(VoteEnum vote)
    {
      var processed = VoteEnum.None;
      foreach (var possiblleVote in Enums.GetValues<VoteEnum>(EnumMemberSelection.DisplayOrder | EnumMemberSelection.Distinct))
      {
        if (vote.HasAllFlags(possiblleVote) && !processed.HasAllFlags(possiblleVote))
        {
          processed |= possiblleVote;
          yield return possiblleVote;
        }
      }
    }
    
    public static StringBuilder Format(this VoteEnum vote, StringBuilder builder) =>
      builder.AppendJoin("", GetFlags(vote).Select(_ => _.AsString(EnumFormat.DisplayName)));
  }
}