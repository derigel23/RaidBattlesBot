using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace RaidBattlesBot.Model
{
  public enum VoteEnum : int
  {
    None = 0,

    [Description("❤")]
    Valor = 1,

    [Description("💛")]
    Instinct = 2,

    [Description("💙")]
    Mystic = 3,

    [Description("💤")]
    MayBe = 4,

    [Description("✖")]
    Cancel = 5
  }
}