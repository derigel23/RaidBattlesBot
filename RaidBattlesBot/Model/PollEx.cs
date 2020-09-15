using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using EnumsNET;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Handlers;
 using Telegram.Bot.Types;
 using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Model
{
  public static class PollEx
  {
    public readonly struct VoteGrouping
    {
      public readonly VoteEnum Flag;
      public readonly int Order;
      public readonly int DisplayOrder;
      public readonly string Singular;
      public readonly string Plural;
      public readonly VoteGrouping[] NestedGrouping;

      public VoteGrouping(VoteEnum flag, int order, int displayOrder, string singular, string plural, params VoteGrouping[] nestedGrouping):
        this(flag, order, displayOrder, singular, plural)
      {
        NestedGrouping = nestedGrouping;
      }

      public VoteGrouping(VoteEnum flag, int order, int displayOrder, string singular, string plural)
      {
        Order = order;
        DisplayOrder = displayOrder;
        Singular = singular;
        Plural = plural;
        Flag = flag;
        NestedGrouping = null;
      }
    }
    
    private static readonly VoteGrouping[] ourVoteGrouping =
    {
      new VoteGrouping(VoteEnum.Going, 1, 1, "is going", "are going", 
        new VoteGrouping(VoteEnum.Going, 3, 1,  "on-site", "on-site"),
        new VoteGrouping(VoteEnum.Remotely, 2, 2, "remotely", "remotely"),
        new VoteGrouping(VoteEnum.Invitation, 1, 3, "by invitation", "by invitation")),
      new VoteGrouping(VoteEnum.Thinking, 2, 2, "maybe", "maybe"),
      new VoteGrouping(VoteEnum.ChangedMind, 3, 3, "bailed", "bailed"),
      new VoteGrouping(VoteEnum.ThumbsUp, 4, 4, "voted for", "votes for"),
      new VoteGrouping(VoteEnum.ThumbsDown, 5, 5, "vote against", "votes against"),
    };

    public static Uri GetThumbUrl(this Poll poll, IUrlHelper urlHelper)
    {
      var thumbnail = default(Uri);
      if (poll.Portal is { } portal)
      {
        thumbnail = portal.GetImage(urlHelper, 64, false);
      }
      return thumbnail ?? poll.Raid().GetThumbUrl(urlHelper);
    }

    private static StringBuilder GetTitleBase(this Poll poll, ParseMode mode)
    {
      var result = poll.Raid()?.GetDescription(mode) ?? new StringBuilder();
      if (poll.Time != null)
      {
        if (result.Length > 0)
          result.Insert(0, RaidEx.Delimeter);
        result.Insert(0, new StringBuilder().Bold((builder, m) => builder.Sanitize($"Бой {poll.Time:t}", m), mode));
      }

      return result;
    }

    public static string GetTitle(this Poll poll, IUrlHelper urlHelper, ParseMode mode = ParseMode.Default)
    {
      var title = poll.GetTitleBase(mode) ?? new StringBuilder();
      if (title.Length == 0)
      {
        title.Bold((builder, m) => builder.Sanitize(poll.Title ?? $"Poll{poll.Id}", m), mode);
        if (poll.Portal != null)
        {
          title.Sanitize(poll.ExRaidGym ? " ☆\u00A0" : " ◊\u00A0", mode);
          title.Bold((builder, m) => builder.Sanitize(poll.Portal.Name, m), mode);
        }
      }

      return title.ToString();
    }

    public static StringBuilder GetDescription(this Poll poll, IUrlHelper urlHelper, ParseMode mode = Helpers.DefaultParseMode)
    {
      var description = poll.GetTitleBase(mode);
      if (!string.IsNullOrEmpty(poll.Title))
      {
        if (description.Length > 0)
        {
          description.NewLine().Sanitize(poll.Title, mode);
        }
        else
        {
          description.Bold((builder, m) => builder.Sanitize(poll.Title, m), mode);
          if (poll.Portal is { } portal)
          {
            description.Sanitize(poll.ExRaidGym ? " ☆\u00A0" : " ◊\u00A0", mode);
            description.Link(portal.Name, $"https://pogo.tools/{portal.Guid}", mode);
            if (poll.ExRaidGym)
            {
              description.Sanitize(" (EX Raid Gym)", mode);
            }
          }
        }
      }
      switch (mode)
      {
        case ParseMode.Html:
        case ParseMode.Markdown:
          var raid = poll.Raid();
          if (raid?.Lat != null && raid?.Lon != null)
          {
            description
              .Sanitize(/*string.IsNullOrEmpty(poll.Title) ? Environment.NewLine : */RaidEx.Delimeter, mode)
              .Link("Map", raid.GetLink(urlHelper), mode);
          }
          break;
      }

      return description;
    }

    public static InputTextMessageContent GetMessageText(this Poll poll, IUrlHelper urlHelper, ParseMode parseMode = Helpers.DefaultParseMode, bool disableWebPreview = false, Func<User, StringBuilder, ParseMode, StringBuilder> userFormatter = null)
    {
      var text = poll.GetDescription(urlHelper, parseMode).NewLine();
      text.Append(" "); // for better presentation in telegram pins & notifications
      
      if (poll.Cancelled)
      {
        text
          .NewLine()
          .Bold((builder, m) => builder.Sanitize("Cancellation!", m).NewLine(), parseMode);
      }

      var compactMode = poll.Votes?.Count > 10;
      var pollVotes = poll.Votes ?? Enumerable.Empty<Vote>();
      GroupVotes(text, pollVotes, ourVoteGrouping);

      text.Link("\x200B", poll.Raid()?.GetLink(urlHelper), parseMode);

      return text.ToTextMessageContent(parseMode, disableWebPreview);
      
      int GroupVotes(StringBuilder result, IEnumerable<Vote> enumerable, IEnumerable<VoteGrouping> grouping, string extraPhrase = null)
      {
        int groupsCount = 0;
        foreach (var voteGroup in enumerable
          .GroupBy(vote => grouping.OrderBy(_ => _.Order).FirstOrDefault(_ => vote.Team?.HasAnyFlags(_.Flag) ?? false))
          .OrderBy(voteGroup => voteGroup.Key.DisplayOrder))
        {
          groupsCount++;
          var votesNumber = voteGroup.Aggregate(0, (i, vote) => i + vote.Team.GetPlusVotesCount() + 1);
          var countStr = votesNumber == 1 ? voteGroup.Key.Singular : voteGroup.Key.Plural;
          StringBuilder FormatCaption(StringBuilder sb)
          {
            var captionParts = new[] { votesNumber.ToString(), extraPhrase, countStr }.Where(s => !string.IsNullOrWhiteSpace(s));
            return sb
              .NewLine()
              .Sanitize(string.Join(" ", captionParts), parseMode)
              .NewLine();
          }

          if (voteGroup.Key.NestedGrouping is {} nestedGrouping)
          {
            var nestedResult = new StringBuilder();
            if (GroupVotes(nestedResult, voteGroup, nestedGrouping) > 1)
            {
              FormatCaption(result).Append(nestedResult);
            }
            else
            {
              GroupVotes(result, voteGroup, nestedGrouping, countStr);
            }
            continue;
          }

          FormatCaption(result);

          foreach (var vote in voteGroup.GroupBy(_ => _.Team?.RemoveFlags(VoteEnum.Modifiers)).OrderBy(_ => _.Key))
          {
            var votes = vote.OrderBy(v => v.Modified);
            if (compactMode)
            {
              result
                .Sanitize(vote.Key?.Description()).Append('\x00A0')
                .AppendJoin(", ", votes.Select(v => v.GetUserLinkWithPluses(userFormatter, parseMode)))
                .NewLine();
            }
            else
            {
              result
                .AppendJoin(Helpers.NewLineString,
                  votes.Select(v => $"{v.Team?.Description().Sanitize(parseMode)} {v.GetUserLinkWithPluses(userFormatter, parseMode)}"))
                .NewLine();
            }
          }
        }

        return groupsCount;
      }
    }

    public static InlineKeyboardMarkup GetReplyMarkup(this Poll poll, PollMode? pollMode = null)
    {
      if (poll.Cancelled)
        return null;

      var pollId = poll.GetId();

      var buttons = new List<IReadOnlyCollection<InlineKeyboardButton>>
      {
        new List<InlineKeyboardButton>(VoteEnumEx.GetFlags(poll.AllowedVotes ?? VoteEnum.Standard)
          .Select(vote =>
          {
            string display = null;
            var votePollModes = vote.GetPollModes();
            if (votePollModes.Length > 1)
            {
                int enabledFlag = -1;
                for (var i = 0; i < votePollModes.Length; i++)
                {
                  if (enabledFlag < 0 && (pollMode?.HasFlag(votePollModes[i].Value) ?? false))
                  {
                    enabledFlag = i;
                  }
                }

                display = votePollModes[++enabledFlag % votePollModes.Length].Key.AsString(EnumFormat.DisplayName);
            }

            display ??= vote.AsString(EnumFormat.DisplayName);
            return vote switch
            {
              VoteEnum.Share => InlineKeyboardButton.WithSwitchInlineQuery(display,$"{ShareInlineQueryHandler.ID}:{pollId}"),
              _ => InlineKeyboardButton.WithCallbackData(display, $"{VoteCallbackQueryHandler.ID}:{pollId}:{vote}")
            };
          }))
      };

      if (pollMode?.HasFlag(PollMode.Invitation) ?? false)
      {
        buttons.Add(new[]
        {
          InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Invite", $"{InviteInlineQueryHandler.PREFIX}{pollId}")
        });
        buttons.Add(new[]
        {
          InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Notify", $"{NotifyInlineQueryHandler.PREFIX}{pollId}")
        });
      }

      return new InlineKeyboardMarkup(buttons);
    }

    public static IQueryable<Poll> IncludeRelatedData(this IQueryable<Poll> polls)
    {
      return polls
        .Include(_ => _.Votes)
        .Include(_ => _.Messages)
        .Include(_ => _.Portal)
        .Include(_ => _.Raid)
        .ThenInclude(raid => raid.PostEggRaid);
    }

    public static int? GetRaidId([CanBeNull] this Poll poll)
    {
      return poll?.Raid?.Id ?? poll?.RaidId;
    }

    public static bool DisableWebPreview([CanBeNull] this Poll poll)
    {
      return GetRaidId(poll) == null;
    }

    public static Raid Raid(this Poll poll)
    {
      return poll.Raid?.PostEggRaid ?? poll.Raid;
    }

    public static InlineQueryResultArticle ClonePoll(this Poll poll, IUrlHelper urlHelper, PollMode? pollMode = null)
    {
      return new InlineQueryResultArticle(poll.GetInlineId(pollMode), poll.GetTitle(urlHelper),
        poll.GetMessageText(urlHelper, disableWebPreview: poll.DisableWebPreview()))
      {
        Description = pollMode?.HasFlag(PollMode.Invitation) ?? false ? "Clone the poll in invitation mode" : "Clone the poll",
        HideUrl = true,
        ThumbUrl = pollMode?.HasFlag(PollMode.Invitation) ?? false ? urlHelper.AssetsContent("static_assets/png/btn_new_party.png").ToString():
          poll.GetThumbUrl(urlHelper).ToString(),
        ReplyMarkup = poll.GetReplyMarkup(pollMode)
      };
    }

    public static PollId GetId(this Poll poll) =>
      new PollId { Id = poll.Id, Format = poll.AllowedVotes ?? VoteEnum.Standard};

    public const string InlineIdPrefix = "poll";
    
    public static string GetInlineId(this Poll poll, PollMode? pollMode = null, int? suffixNumber = null) =>
      $"{InlineIdPrefix}:{poll.GetId()}:{poll.Portal?.Guid ?? poll.PortalId}:{pollMode?.AsString(EnumFormat.HexadecimalValue)}:{suffixNumber}";

    public static bool TryGetPollId(ReadOnlySpan<char> text, out int pollId, out VoteEnum? format)
    {
      if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out pollId))
      {
        if (!PollId.TryRead(text, out var pollIdCombined))
        {
          format = VoteEnum.None;
          return false;
        }
        
        pollId = pollIdCombined.Id;
        format = pollIdCombined.Format;
      }
      else
      {
        format = null;
      }

      return true;
    }
  }
}