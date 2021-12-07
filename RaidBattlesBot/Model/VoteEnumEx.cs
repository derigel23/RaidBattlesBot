using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EnumsNET;
using Team23.TelegramSkeleton;

namespace RaidBattlesBot.Model
{
    public static class VoteEnumEx
    {
        private static readonly IDictionary<VoteEnum, PollMode?> ourVoteToPollMode;

        static VoteEnumEx()
        {
            ourVoteToPollMode = Enums.GetMembers<VoteEnum>().Aggregate(new Dictionary<VoteEnum, PollMode?>(),
                (modes, member) =>
                {    
                    var pollMode = member.Attributes.Get<PollModeAttribute>()?.PollMode;
                    if (modes.TryGetValue(member.Value, out var prevPollMode))
                    {
                        modes[member.Value] = pollMode | prevPollMode;
                    }
                    else
                    {
                        modes.Add(member.Value, pollMode);
                    }

                    return modes;
                });
        }

        public static PollMode? GetPollMode(this VoteEnum vote) =>
            ourVoteToPollMode.TryGetValue(vote, out var pollMode) ? pollMode : default;

        public static KeyValuePair<VoteEnum, PollMode>[] GetPollModes(this VoteEnum vote) =>
            vote
                .GetFlags()
                .Select(v => KeyValuePair.Create(v, v.GetPollMode())).Where(_ => _.Value != null)
                .DefaultIfEmpty(KeyValuePair.Create(vote, vote.GetPollMode())).Where(_ => _.Value != null)
                .Select(pair => KeyValuePair.Create(pair.Key, pair.Value.Value))
                .OrderBy(pair => pair.Key.GetAttributes()?.Get<DisplayAttribute>()?.GetOrder())
                .ToArray();
    
        public static readonly ICollection<VoteEnum> DefaultVoteFormats = new []
        {
            // hearts
            VoteEnum.Standard,
      
            // classic
            VoteEnum.TeamPlusOne | VoteEnum.MayBe | VoteEnum.Cancel | VoteEnum.Share,
      
            // thumbs up/down
            VoteEnum.Thumbs | VoteEnum.Share
        };

        private static readonly int FirstPlusBit = (int)Math.Log((int)VoteEnum.Plus1, 2);

        public static int GetPlusVotesCount(this VoteEnum? vote) =>
            ((int)(vote?.CommonFlags(VoteEnum.Plus) ?? VoteEnum.None) >> FirstPlusBit);

        // TODO: check for overflow
        public static VoteEnum IncreaseVotesCount(this VoteEnum vote, int diff) => vote
            .RemoveFlags(VoteEnum.Plus)
            .CombineFlags((VoteEnum) ((((int)vote.CommonFlags(VoteEnum.Plus) >> FirstPlusBit) + diff) << FirstPlusBit));

        public static string Description(this VoteEnum vote) =>
            (vote.RemoveFlags(VoteEnum.Modifiers) is var voteWithoutModifiers && voteWithoutModifiers.HasAnyFlags() ?
                voteWithoutModifiers : vote.HasAnyFlags(VoteEnum.Modifiers) ? VoteEnum.Yes : vote).AsString(EnumFormat.DisplayName);

        public static IEnumerable<VoteEnum> GetFlags(VoteEnum vote)
        {
            var processed = VoteEnum.None;
            foreach (var possibleVote in Enums.GetValues<VoteEnum>(EnumMemberSelection.DisplayOrder | EnumMemberSelection.Distinct))
            {
                if (vote.HasAllFlags(possibleVote) && !processed.HasAllFlags(possibleVote))
                {
                    processed |= possibleVote;
                    yield return possibleVote;
                }
            }
        }
    
        public static TextBuilder Format(this VoteEnum vote, TextBuilder builder) =>
          GetFlags(vote).Aggregate(builder, (b, v) => b.Append(v.AsString(EnumFormat.DisplayName)));
    }
}