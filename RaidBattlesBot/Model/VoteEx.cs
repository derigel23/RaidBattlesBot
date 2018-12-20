using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Model
{
  public static class VoteEx
  {
    public static string GetUserLinkWithPluses(this Vote vote, ParseMode mode = Helpers.DefaultParseMode)
    {
      var result = vote.User.GetLink(mode);

      if (vote.Team.GetPlusVotesCount() is var plusCount && plusCount > 0)
      {
        result
          .Sanitize("⁺", mode)
          .Sanitize(
            ourSuperScriptNumbers.Aggregate(plusCount.ToString(), (s, nums) => s.Replace(nums.Key, nums.Value)), mode);
      }

      return result.ToString();
    }

    private static readonly IReadOnlyDictionary<char, char> ourSuperScriptNumbers = new Dictionary<char, char>()
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