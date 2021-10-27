using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;
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
      new(VoteEnum.Host, 0, 0,  "Host", "Hosts"),
      new(VoteEnum.Going, 1, 1, "{0} is going", "{0} are going", 
        new (VoteEnum.Hosting, 3, 1,  "{0} on-site", "{0} on-site"),
        new (VoteEnum.Remotely, 2, 2, "{0} remotely", "{0} remotely"),
        new (VoteEnum.Invitation, 1, 3, "{0} by invitation", "{0} by invitation")),
      new(VoteEnum.Thinking, 2, 2, "{0} maybe", "{0} maybe"),
      new(VoteEnum.ChangedMind, 3, 3, "{0} bailed", "{0} bailed"),
      new(VoteEnum.ThumbsUp, 4, 4, "{0} voted for", "{0} votes for"),
      new(VoteEnum.ThumbsDown, 5, 5, "{0} vote against", "{0} votes against"),
      new(VoteEnum.Thanks, 6, 6, "{0} thanked", "{0} thanked"),
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

    private static StringBuilder GetTitleBase(this Poll poll, StringBuilder builder, ParseMode mode)
    {
      var result = poll.Raid()?.GetDescription(builder, mode) ?? builder;
      // if (poll.Time != null)
      // {
      //   if (result.Length > 0)
      //     result.Insert(0, RaidEx.Delimeter);
      //   result.Insert(0, new StringBuilder().Bold((builder, m) => builder.Sanitize($"Battle at {poll.Time:t}", m), mode));
      // }

      return result;
    }

    public static string GetTitle(this Poll poll, ParseMode mode = 0) // by default no formatting at all
    {
      return GetTitle(poll, new StringBuilder(), mode).ToString();
    }

    public static StringBuilder GetTitle(this Poll poll, StringBuilder builder, ParseMode mode = Helpers.DefaultParseMode)
    {
      var initialLength = builder.Length;
      var title = poll.GetTitleBase(builder, mode);
      if (title.Length == initialLength)
      {
        title.Bold((b, m) => b.Sanitize(poll.Title ?? $"Poll{poll.Id}", m), mode);
        if (poll.Portal is { } portal)
        {
          title.Sanitize(poll.ExRaidGym ? " ☆\u00A0" : " ◊\u00A0", mode);
          title.Link(portal.Name, $"https://pogo.tools/{portal.Guid}", mode);
        }
      }

      return title;
    }

    public static StringBuilder GetDescription(this Poll poll, IUrlHelper urlHelper, ParseMode mode = Helpers.DefaultParseMode)
    {
      var description = poll.GetTitleBase(new StringBuilder(), mode);
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
          if (raid?.Lat != null && raid?.Lon != null && urlHelper != null)
          {
            description
              .Sanitize(/*string.IsNullOrEmpty(poll.Title) ? Environment.NewLine : */RaidEx.Delimeter, mode)
              .Link("Map", raid.GetLink(urlHelper), mode);
          }
          break;
      }

      return description;
    }

    public static InputTextMessageContent GetMessageText(this Poll poll, IUrlHelper urlHelper, ParseMode parseMode = Helpers.DefaultParseMode, bool disableWebPreview = false,
      Func<User, StringBuilder, ParseMode, StringBuilder> userFormatter = null, Func<IGrouping<VoteEnum?, Vote>, bool, StringBuilder, ParseMode, StringBuilder> userGroupFormatter = null)
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
            return sb
              .NewLine()
              .Sanitize(string.Format(countStr, extraPhrase is {} extraPhraseNotNull ? string.Format(extraPhraseNotNull, votesNumber) : votesNumber), parseMode)
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
            if (userGroupFormatter != null)
            {
              userGroupFormatter(vote, compactMode, result, parseMode);
              continue;
            }
            
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
        new List<InlineKeyboardButton>(collection: VoteEnumEx.GetFlags(poll.AllowedVotes & ~VoteEnum.ImplicitVotes ?? VoteEnum.Standard)
          .Select(vote =>
          {
            string display = null;
            var votePollModes = vote.GetPollModes();
            if (votePollModes.Length > 1)
            {
                int enabledFlag = -1;
                for (var i = 0; i < votePollModes.Length; i++)
                {
                  if (enabledFlag < 0 && (pollMode ?? PollMode.Default).HasFlag(votePollModes[i].Value))
                  {
                    enabledFlag = i;
                  }
                }

                if (enabledFlag >= 0)
                {
                  display = votePollModes[++enabledFlag % votePollModes.Length].Key.AsString(EnumFormat.DisplayName);
                }
            }

            display ??= vote.AsString(EnumFormat.DisplayName);
            return vote switch
            {
              VoteEnum.Share => InlineKeyboardButton.WithSwitchInlineQuery(display,$"{ShareInlineQueryHandler.ID}:{pollId}"),
              _ => InlineKeyboardButton.WithCallbackData(display, $"{VoteCallbackQueryHandler.ID}:{pollId}:{vote.AsString(EnumFormat.HexadecimalValue)}")
            };
          }))
      };

      if (pollMode?.HasFlag(PollMode.Invitation) ?? false)
      {
        buttons.Add(new[]
        {
          InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Invite", $"{InviteInlineQueryHandler.PREFIX}{pollId}")
        });
      }

      return new InlineKeyboardMarkup(buttons);
    }

    public static IQueryable<Poll> IncludeRelatedData(this IQueryable<Poll> polls)
    {
      return polls
        .AsSingleQuery()
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
      return new InlineQueryResultArticle(poll.GetInlineId(pollMode), poll.GetTitle(),
        poll.GetMessageText(urlHelper, disableWebPreview: poll.DisableWebPreview()))
      {
        Description = pollMode?.HasFlag(PollMode.Invitation) ?? false ? "Clone the poll in invitation mode" : "Clone the poll",
        HideUrl = true,
        ThumbUrl = pollMode?.HasFlag(PollMode.Invitation) ?? false ? urlHelper.AssetsContent("static_assets/png/btn_new_party.png").ToString():
          poll.GetThumbUrl(urlHelper).ToString(),
        ReplyMarkup = poll.GetReplyMarkup(pollMode)
      };
    }

    public static PollId GetId(this Poll poll) => new()
      { Id = poll.Id, Format = poll.AllowedVotes ?? VoteEnum.Standard };

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
    
    private static readonly Regex ourRaidTimeDetector =
      new(
        @"(^|\s|\b)(?<time>\d{1,2}(?<delimeter>[-:.])\d{2})\s*(?<designator>[aApP]\.?[mM]\.?)?\s*(?<timezone>[\p{L}\p{N}/_\-\+]{2,}(?![-:.]))?(\b|\s|$)");
    
    public static async Task<Poll> DetectRaidTime(this Poll poll, TimeZoneService timeZoneService, Func<Task<Location>> getLocation, Func<CancellationToken, Task<ZonedDateTime>> getDateTime, CancellationToken cancellationToken = default)
    {
      if (ourRaidTimeDetector.Matches(poll.Title) is {} matches && matches.LastOrDefault() is {} match)
      {
        try
        {
          var timePattern = LocalTimePattern.CreateWithCurrentCulture(@$"H\{match.Groups["delimeter"]}mm");
          if (timePattern.Parse(match.Groups["time"].Value).TryGetValue(LocalTime.MinValue, out var time))
          {
            if (match.Groups["timezone"] is { Success: true } timezoneMatch)
            {
              if (!timeZoneService.TryGetTimeZoneByAbbreviation(timezoneMatch.Value, await getLocation(), out var timeZone))
              {
                if (OffsetPattern.CreateWithCurrentCulture("g").Parse(timezoneMatch.Value).TryGetValue(Offset.Zero, out var offset))
                {
                  timeZone = DateTimeZone.ForOffset(offset);
                }
              }

              if (timeZone != null)
              {
                var getPrevDateTime = getDateTime;
                getDateTime = async ct => (await getPrevDateTime(ct)).WithZone(timeZone);
              }
            }

            if (match.Groups["designator"] is { Success: true } designatorMatch)
            {
              if (time.ClockHourOfHalfDay == 12)
              {
                time = time.PlusHours(-12);
              }

              if (designatorMatch.Value.Contains("p", StringComparison.OrdinalIgnoreCase))
              {
                time = time.PlusHours(12);
              }
            }

            var ((date, _), dateTimeZone, _) = await getDateTime(cancellationToken);
            var localDateTime = time.On(date);
            var detectedTime = localDateTime.InZoneLeniently(dateTimeZone);
            poll.Time = detectedTime.ToDateTimeOffset();
            poll.TimeZoneId = detectedTime.Zone.Id;
          }
        }
        catch (ArgumentOutOfRangeException)
        {
          // ignored
        }
      }
      return poll;
    }

    public static Poll InitImplicitVotes([CanBeNull] this Poll poll, User owner, long? botId = null)
    {
      if (poll == null) return null;
      var allowedImplicitVotes = (poll.AllowedVotes ?? VoteEnum.None) & VoteEnum.ImplicitVotes;
      // implicit votes are not allowed for poll
      if (allowedImplicitVotes == 0) return poll;
      // there is already vote for specified user
      if (poll.Votes?.Any(vote => vote.UserId == owner.Id) ?? false) return poll;
      (poll.Votes ??= new List<Vote>()).Add(new Vote
      {
        BotId = botId,
        User = owner,
        Team = allowedImplicitVotes, // all implicit votes at once?! 🤷
        PollId = poll.Id
      });

      return poll;
    }

    public static async Task<InputTextMessageContent> GetInviteMessage(this Poll poll, RaidBattlesContext db, CancellationToken cancellationToken = default)
    {
      if (!poll.AllowedVotes?.HasFlag(VoteEnum.Invitation) ?? true)
        return null;

      var inviteVotes = poll.Votes
        .Where(vote => vote.Team?.HasFlag(VoteEnum.Invitation) ?? false)
        .OrderBy(vote => vote.Modified)
        .ToList();

      var invitees = inviteVotes.Select(vote => vote.UserId).ToList();
      var nicknames = (await db.Set<Player>()
          .Where(player => invitees.Contains(player.UserId))
          .ToListAsync(cancellationToken))
        .ToDictionary(player => player.UserId, player => player.Nickname);

      var resultNicknames = inviteVotes
        .Select(vote => (nicknames.TryGetValue(vote.UserId, out var nickname) ? nickname : null) ?? vote.Username)
        .Where(_ => !string.IsNullOrEmpty(_))
        .ToList();

      if (resultNicknames.Count > 0)
      {
        return
          new StringBuilder()
            .Code((builder, mode) => builder.Sanitize(string.Join(",", resultNicknames), mode))
            .ToTextMessageContent();
      }

      return null;
    }
  }
}