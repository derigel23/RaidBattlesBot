using System;
using System.ComponentModel.DataAnnotations;

namespace RaidBattlesBot.Model
{
  [Flags]
  public enum VoteEnum : int
  {
    None = 0,

    [Display(Name = "✅", Order = 10)]
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

    [Display(Name = "❌", Order = 100, Description = "You've bailed!")]
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
    
    [Display(Name = "📡", Order = 11, Description = "You're going to participate remotely")]
    Remotely = Professor << 1,

    [Display(Name = "💚", Order = 10, Description = "You're voted")]
    TeamHarmony = Remotely << 1,

    [Display(Name = "💌", Order = 12, Description = "You need an invitation")]
    Invitation = TeamHarmony << 1,
    
    [Display(Name = "🆔", Order = 107, Description = "Show IGNs"), PollModeAttribute(Model.PollMode.Nicknames)]
    PollModeNicknames = Invitation << 1,
    [Display(Name = "🔠", Order = 107, Description = "Show Names"), PollModeAttribute(Model.PollMode.Names)]
    PollModeNames = PollModeNicknames << 1,
    
    [Display(Name = "🙏", Order = 101, Description = "You've thanked!")]
    Thanks = PollModeNames << 2,

    #region Plused votes

    [Display(Name = "❤", Order = 1)]
    ValorPlusOne = Valor | Plus1,
    [Display(Name = "💛", Order = 1)]
    InstinctPlusOne = Instinct | Plus1,
    [Display(Name = "💙", Order = 1)]
    MysticPlusOne = Mystic | Plus1,

    [Display(Name = "✅", Order = 1)]
    YesPlus1 = Yes | Plus1,

    [Display(Name = "👍", Order = 1)]
    ThumbsUpPlus1 = ThumbsUp | Plus1,
    [Display(Name = "👎", Order = 1)]
    ThumbsDownPlus1 = ThumbsDown | Plus1,

    [Display(Name = "🥊", Order = 1)]
    AurorPlusOne = Auror | Plus1,
    [Display(Name = "🦎", Order = 1)]
    MagizoologistPlusOne = Magizoologist | Plus1,
    [Display(Name = "🧙‍♂", Order = 1)]
    ProfessorPlusOne = Professor | Plus1,
    
    [Display(Name = "📡", Order = 2)]
    RemotelyPlusOne = Remotely | Plus1,
    
    [Display(Name = "💚", Order = 1)]
    TeamHarmonyPlusOne = TeamHarmony | Plus1,

    [Display(Name = "💌", Order = 3)]
    InvitationPlusOne = Invitation | Plus1,
    
    #endregion

    Standard = YesPlus1 | Remotely | Invitation | MayBe | Cancel | PollMode | Share,

    Team = Valor | Instinct | Mystic,
    TeamPlusOne = Team | Plus1,
    HarryPotter = Auror | Magizoologist | Professor,
    Going = Yes | Team | HarryPotter | TeamHarmony | Remotely | Invitation,
    Thinking = MayBe,
    Countable = Going | Thumbs,
    Some = Countable | MayBe,
    SomePlus = Some | Plus,
    ChangedMind = Cancel,

    Plus = Plus1 | Plus2 | Plus4 | Plus8,
    
    [Display(Name = "🆔", Order = 106, Description = "Show IGNs / Names")]
    PollMode = PollModeNicknames | PollModeNames,
    
    Modifiers = Plus | Share | PollMode,
  }
}