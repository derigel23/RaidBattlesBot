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

    [Display(Name = "❤", Order = 10)]
    Valor = Yes << 1,

    [Display(Name = "💛", Order = 10)]
    Instinct = Valor << 1,

    [Display(Name = "💙", Order = 10)]
    Mystic = Instinct << 1,

    [Display(Name = "⁺¹", Order = 15)]
    Plus1 = Mystic << 1,

    [Display(Name = "+2", Order = 15)]
    Plus2 = Plus1 << 1,

    [Display(Name = "+4", Order = 15)]
    Plus4 = Plus2 << 1,

    [Display(Name = "+8", Order = 15)]
    Plus8 = Plus4 << 1,

    [Display(Name = "💤", Order = 20)]
    MayBe = Plus8 << 1,

    [Display(Name = "✖", Order = 100)]
    Cancel = MayBe << 1,

    [Display(Name = "🌐", Order = 9999)]
    Share = Cancel << 1,

    [Display(Name = "👍", Order = 10)]
    ThumbsUp = Share << 1,
    
    [Display(Name = "👎", Order = 10)]
    ThumbsDown = ThumbsUp << 1,
    
    Thumbs = ThumbsUp | ThumbsDown,
    
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

    #endregion

    Standard = ValorPlusOne | InstinctPlusOne | MysticPlusOne | MayBe | Cancel | Share,

    Team = Valor | Instinct | Mystic,
    Going = Yes | Team,
    Thinking = MayBe,
    Countable = Going | Thumbs,
    Some = Thumbs | MayBe ,
    SomePlus = Some | Plus,
    ChangedMind = Cancel,

    Plus = Plus1 | Plus2 | Plus4 | Plus8
  }

  public static class VoteEnumEx
  {
    public static readonly ICollection<VoteEnum> DefaultVoteFormats = new []
    {
      // hearts
      VoteEnum.Standard,
      
      // compact
      VoteEnum.YesPlus1 | VoteEnum.MayBe | VoteEnum.Cancel | VoteEnum.Share,
      
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
      (vote.RemoveFlags(VoteEnum.Plus) is VoteEnum voteWithoutPlus && voteWithoutPlus.HasAnyFlags() ?
        voteWithoutPlus : vote.HasAnyFlags(VoteEnum.Plus) ? VoteEnum.Yes : vote).AsString(EnumFormat.DisplayName, EnumFormat.Description);

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
      builder.AppendJoin("", GetFlags(vote).Select(_ => _.AsString(EnumFormat.DisplayName, EnumFormat.Description)));
  }
}