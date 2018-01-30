using System;
using System.ComponentModel;
using EnumsNET;

namespace RaidBattlesBot.Model
{
  [Flags]
  public enum VoteEnum : int
  {
    None = 0,

    [Description("✔")]
    Yes = 1 << 0,

    [Description("❤")]
    Valor = Yes << 1,

    [Description("💛")]
    Instinct = Valor << 1,

    [Description("💙")]
    Mystic = Instinct << 1,

    [Description("✚𝟭")]
    Plus1 = Mystic << 1,

    [Description("✚𝟮")]
    Plus2 = Plus1 << 1,

    [Description("✚𝟰")]
    Plus4 = Plus2 << 1,

    [Description("✚𝟴")]
    Plus8 = Plus4 << 1,

    [Description("💤")]
    MayBe = Plus8 << 1,

    [Description("✖")]
    Cancel = MayBe << 1,

    Standard = Valor | Instinct | Mystic | MayBe | Cancel,
    Compact =  Yes | Plus1 | MayBe | Cancel,
    Minimal =  Yes | Plus1 | Cancel,

    Going = Yes | Valor | Instinct | Mystic,
    Thinking = MayBe,
    SomePlus = Going | MayBe | Plus,
    ChangedMind = Cancel,

    Plus = Plus1 | Plus2 | Plus4 | Plus8
  }

  public static class VoteEnumEx
  {
    private static readonly int FirstPlusBit = (int)Math.Log((int)VoteEnum.Plus1, 2);

    public static int GetPlusVotesCount(this VoteEnum? vote) =>
      ((int)(vote?.CommonFlags(VoteEnum.Plus) ?? VoteEnum.None) >> FirstPlusBit);

    // TODO: check for overflow
    public static VoteEnum IncreaseVotesCount(this VoteEnum vote, int diff) => vote
      .RemoveFlags(VoteEnum.Plus)
      .CombineFlags((VoteEnum) ((((int)(vote.CommonFlags(VoteEnum.Plus)) >> FirstPlusBit) + diff) << FirstPlusBit));

    public static string Description(this VoteEnum vote) =>
      (vote.RemoveFlags(VoteEnum.Plus) is VoteEnum voteWithoutPlus && voteWithoutPlus.HasAnyFlags() ?
        voteWithoutPlus : vote.HasAnyFlags(VoteEnum.Plus) ? VoteEnum.Yes : vote).AsString(EnumFormat.Description);
  }
}