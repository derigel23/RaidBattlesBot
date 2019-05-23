﻿using System;
using System.Collections.Generic;
 using System.Globalization;
 using System.Linq;
using System.Text;
using EnumsNET;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
 using RaidBattlesBot.Handlers;
 using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
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
      var thumbnail = default(Uri);
      if (poll.Portal is Portal portal)
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
          if (poll.Portal is Portal portal)
          {
            description.Sanitize(poll.ExRaidGym ? " ☆\u00A0" : " ◊\u00A0", mode);
            description.Link(portal.Name, urlHelper.RouteUrl("Portal", new { guid = portal.Guid }, "https"), mode);
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
              .Link("Карта", raid.GetLink(urlHelper), mode);
          }
          break;
      }

      return description;
    }

    public static InputTextMessageContent GetMessageText(this Poll poll, IUrlHelper urlHelper, ParseMode mode = Helpers.DefaultParseMode, bool disableWebPreview = false)
    {
      var text = poll.GetDescription(urlHelper, mode).NewLine();
      
      if (poll.Cancelled)
      {
        text
          .NewLine()
          .Bold((builder, m) => builder.Sanitize("Отмена!", m).NewLine(), mode);
      }

      var compactMode = poll.Votes?.Count > 10;
      foreach (var voteGroup in (poll.Votes ?? Enumerable.Empty<Vote>())
        .GroupBy(vote => ourVoteDescription.FirstOrDefault(_ => vote.Team?.HasAnyFlags(_.Key) ?? false).Value)
        .OrderBy(voteGroup => voteGroup.Key.Order))
      {
        var votesNumber = voteGroup.Aggregate(0, (i, vote) => i + vote.Team.GetPlusVotesCount() + 1);
        var countStr = votesNumber == 1 ? voteGroup.Key.Singular : voteGroup.Key.Plural;
        text.NewLine().Sanitize($"{votesNumber} {countStr}", mode).NewLine();

        foreach (var vote in voteGroup.GroupBy(_ => _.Team?.RemoveFlags(VoteEnum.Plus | VoteEnum.Share)).OrderBy(_ => _.Key))
        {
          var votes = vote.OrderBy(v => v.Modified);
          if (compactMode)
          {
            text
              .Sanitize(vote.Key?.Description()).Append('\x00A0')
              .AppendJoin(", ", votes.Select(v => v.GetUserLinkWithPluses(mode)))
              .NewLine();
          }
          else
          {
            text
              .AppendJoin(Helpers.NewLineString, votes.Select(v => $"{v.Team?.Description().Sanitize(mode)} {v.GetUserLinkWithPluses(mode)}"))
              .NewLine();
          }
        }
      }

      text.Link("\x200B", poll.Raid()?.GetLink(urlHelper), mode);

      return text.ToTextMessageContent(mode, disableWebPreview);
    }

    public static InlineKeyboardMarkup GetReplyMarkup(this Poll poll)
    {
      if (poll.Cancelled)
        return null;

      var pollId = poll.GetId();
      InlineKeyboardButton GetVoteButton(VoteEnum vote) =>
        InlineKeyboardButton.WithCallbackData(vote.AsString(EnumFormat.DisplayName, EnumFormat.Description), $"{VoteCallbackQueryHandler.ID}:{pollId}:{vote}");

      var buttons = new List<InlineKeyboardButton>(VoteEnumEx.GetFlags(poll.AllowedVotes ?? VoteEnum.Standard)
        .Select(vote =>
        {
          var display = vote.AsString(EnumFormat.DisplayName, EnumFormat.Description);
          switch (vote)
          {
            case VoteEnum.Share:
              return InlineKeyboardButton.WithSwitchInlineQuery(display, $"{ShareInlineQueryHandler.ID}:{pollId}");
            
            default:
              return InlineKeyboardButton.WithCallbackData(display, $"{VoteCallbackQueryHandler.ID}:{pollId}:{vote}");
          }
        }));
      return new InlineKeyboardMarkup(buttons.ToArray());
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

    public static InlineQueryResultArticle ClonePoll(this Poll poll, IUrlHelper urlHelper)
    {
      return new InlineQueryResultArticle($"poll:{poll.GetId()}", poll.GetTitle(urlHelper),
        poll.GetMessageText(urlHelper, disableWebPreview: poll.DisableWebPreview()))
      {
        Description = "Клонировать голосование",
        HideUrl = true,
        ThumbUrl = poll.GetThumbUrl(urlHelper).ToString(),
        ReplyMarkup = poll.GetReplyMarkup()
      };
    }

    public static PollId GetId(this Poll poll) =>
      new PollId { Id = poll.Id, Format = poll.AllowedVotes ?? VoteEnum.Standard};

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