using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Model
{
  public static class VoteEx
  {
    public static StringBuilder GetUserLinkWithPluses(this Vote vote, ParseMode mode = ParseMode.Default)
    {
      var result = vote.User.GetLink(mode);

      if (vote.Team.GetPlusVotesCount() is var plusCount && plusCount > 0)
      {
        result
          .Append('⁺')
          .Append(
            ourSuperScriptNumbers.Aggregate(plusCount.ToString(), (s, nums) => s.Replace(nums.Key, nums.Value)));
      }

      return result;
    }

    public static readonly IReadOnlyDictionary<char, char> ourSuperScriptNumbers = new Dictionary<char, char>()
    {
      { '0', '⁰' },
      { '1', '¹' },
      { '2', '²' },
      { '3', '³' },
      { '4', '⁴' },
      { '5', '⁵' },
      { '6', '⁶' },
      { '7', '⁷' },
      { '8', '⁸' },
      { '9', '⁹' }
    };
  }
}