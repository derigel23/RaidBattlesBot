using System;
using System.Collections.Generic;
using System.Linq;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Model
{
  public static class VoteEx
  {
    public static TextBuilder GetUserLinkWithPluses(this Vote vote, TextBuilder builder, Func<User, TextBuilder, TextBuilder> userFormatter = null)
    {
      var result = vote.User.GetLink(builder, userFormatter ?? UserEx.DefaultUserExtractor);

      if (vote.Team.GetPlusVotesCount() is var plusCount and > 0)
      {
        result
          .Sanitize("⁺")
          .Sanitize(
            ourSuperScriptNumbers.Aggregate(plusCount.ToString(), (s, nums) => s.Replace(nums.Key, nums.Value)));
      }

      return result;
    }

    private static readonly IReadOnlyDictionary<char, char> ourSuperScriptNumbers = new Dictionary<char, char>
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