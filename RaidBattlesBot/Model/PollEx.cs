using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Model
{
  public static class PollEx
  {
    private static readonly Dictionary<VoteEnum, (int Order, string Singular, string Plural)> ourVoteDescription = new Dictionary<VoteEnum, (int, string, string)>
    {
      { VoteEnum.Going, (1, "идёт", "идут") },
      { VoteEnum.Thinking, (2, "думает", "думают") },
      { VoteEnum.ChangedMind, (3, "передумал", "передумали") },
    };

    public static Uri GetThumbUrl(this Poll poll, IUrlHelper urlHelper)
    {
      return  poll.Raid().GetThumbUrl(urlHelper);
    }

    private static StringBuilder GetTitleBase(this Poll poll, ParseMode mode)
    {
      var result = poll.Raid()?.GetDescription(mode) ?? new StringBuilder();
      if (poll.Time != null)
      {
        if (result.Length > 0)
          result.Insert(0, RaidEx.Delimeter);
        result.Insert(0, new StringBuilder().Bold(mode, builder => builder.Append($"Бой {poll.Time:t}")));
      }

      return result;
    }

    public static string GetTitle(this Poll poll, IUrlHelper urlHelper, ParseMode mode = ParseMode.Default)
    {
      var title = poll.GetTitleBase(mode) ?? new StringBuilder();
      if (title.Length == 0)
      {
        title.Bold(mode, builder => builder.Append(poll.Title.Sanitize(mode) ?? $"Poll{poll.Id}"));
      }

      return title.ToString();
    }

    public static StringBuilder GetDescription(this Poll poll, IUrlHelper urlHelper, ParseMode mode = ParseMode.Default)
    {
      var description = poll.GetTitleBase(mode);
      if (!String.IsNullOrEmpty(poll.Title))
      {
        if (description.Length > 0)
          description.AppendLine().Append(poll.Title.Sanitize(mode));
        else
          description.Bold(mode, builder => builder.Append(poll.Title.Sanitize(mode)));
      }
      switch (mode)
      {
        case ParseMode.Html:
        case ParseMode.Markdown:
          var raid = poll.Raid();
          if (raid?.Lat != null && raid?.Lon != null)
          {
            description
              .Append(/*string.IsNullOrEmpty(poll.Title) ? Environment.NewLine : */RaidEx.Delimeter)
              .Link("Карта", raid.GetLink(urlHelper), mode);
          }
          break;
      }

      return description;
    }

    public static async Task<StringBuilder> GetMessageText(this Poll poll, IUrlHelper urlHelper, UserInfo userInfo, ParseMode mode = ParseMode.Default, CancellationToken cancellationToken = default)
    {
      var text = poll.GetDescription(urlHelper, mode).AppendLine();
      
      if (poll.Cancelled)
      {
        text
          .AppendLine()
          .Bold(mode, builder => builder.AppendLine("Отмена!"));
      }

      var compactMode = poll.Votes?.Count > 10;
      foreach (var voteGroup in (poll.Votes ?? Enumerable.Empty<Vote>())
        .GroupBy(vote => ourVoteDescription.FirstOrDefault(_ => vote.Team?.HasAnyFlags(_.Key) ?? false).Value)
        .OrderBy(voteGroup => voteGroup.Key.Order))
      {
        var votesNumber = voteGroup.Aggregate(0, (i, vote) => i + vote.Team.GetPlusVotesCount() + 1);
        var countStr = votesNumber == 1 ? voteGroup.Key.Singular : voteGroup.Key.Plural;
        text.AppendLine().AppendLine($"{votesNumber} {countStr}");

        foreach (var vote in voteGroup.GroupBy(_ => _.Team?.RemoveFlags(VoteEnum.Plus)).OrderBy(_ => _.Key))
        {
          var votes = vote.OrderBy(v => v.Modified);
          if (compactMode)
          {
            text
              .Append(vote.Key?.Description()).Append('\x00A0')
              .AppendJoin(", ", await Task.WhenAll(votes.Select(v => v.GetUserLinkWithPluses(userInfo, mode, cancellationToken))))
              .AppendLine();
          }
          else
          {
            text
              .AppendJoin(Environment.NewLine, await Task.WhenAll(votes.Select(async v => $"{v.Team?.Description()} {await v.GetUserLinkWithPluses(userInfo, mode, cancellationToken)}")))
              .AppendLine();
          }
        }
      }

      return text.Link("\x200B", poll.Raid()?.GetLink(urlHelper), mode);
    }

    public static InlineKeyboardMarkup GetReplyMarkup(this Poll poll)
    {
      if (poll.Cancelled)
        return null;

      var pollId = poll.Id;

      InlineKeyboardCallbackButton GetVoteButton(VoteEnum vote) =>
        new InlineKeyboardCallbackButton(vote.AsString(EnumFormat.Description), $"vote:{pollId}:{vote}");

      var buttons = new List<InlineKeyboardButton>();
      foreach (var voteButton in (poll.AllowedVotes ?? VoteEnum.Standard).GetFlags())
      {
        buttons.Add(GetVoteButton(voteButton));
      }
      buttons.Add(new InlineKeyboardSwitchInlineQueryButton("🌐", $"share:{pollId}"));
      return new InlineKeyboardMarkup(buttons.ToArray());
    }

    public static IQueryable<Poll> IncludeRelatedData(this IQueryable<Poll> polls)
    {
      return polls
        .Include(_ => _.Votes)
        .Include(_ => _.Messages)
        .Include(_ => _.Raid)
        .ThenInclude(raid => raid.PostEggRaid);
    }

    public static int? GetRaidId([CanBeNull] this Poll poll)
    {
      return poll?.Raid?.Id ?? poll?.RaidId;
    }

    public static Raid Raid(this Poll poll)
    {
      return poll.Raid?.PostEggRaid ?? poll.Raid;
    }

    public static async Task<InlineQueryResultArticle> ClonePoll(this Poll poll, IUrlHelper urlHelper, UserInfo userInfo, CancellationToken cancellationToken = default)
    {
      return new InlineQueryResultArticle
      {
        Id = $"poll:{poll.Id}",
        Title = poll.GetTitle(urlHelper),
        Description = "Клонировать голосование",
        HideUrl = true,
        ThumbUrl = poll.GetThumbUrl(urlHelper).ToString(),
        InputMessageContent = new InputTextMessageContent
        {
          MessageText = (await poll.GetMessageText(urlHelper, userInfo, RaidEx.ParseMode, cancellationToken)).ToString(),
          ParseMode = RaidEx.ParseMode,
          DisableWebPagePreview = poll.GetRaidId() == null
        },
        ReplyMarkup = poll.GetReplyMarkup()
      };
    }
  }
}