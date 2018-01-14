using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Model
{
  public static class PollEx
  {
    private static readonly Dictionary<VoteEnum, (int Order, string Singular, string Plural)> ourVoteDescription = new Dictionary<VoteEnum, (int, string, string)>
    {
      { VoteEnum.Valor, (1, "идёт", "идут") },
      { VoteEnum.Instinct, (1, "идёт", "идут") },
      { VoteEnum.Mystic, (1, "идёт", "идут") },
      
      { VoteEnum.MayBe, (2, "думает", "думают") },
      
      { VoteEnum.Cancel, (3, "передумал", "передумали") },
    };

    public static Uri GetThumbUrl(this Poll poll, IUrlHelper urlHelper)
    {
      return  poll.Raid().GetThumbUrl(urlHelper);
    }

    private static StringBuilder GetTitleBase(this Poll poll)
    {
      var result = poll.Raid()?.GetDescription() ?? new StringBuilder();
      if (poll.Time != null)
      {
        if (result.Length > 0)
          result.Insert(0, RaidEx.Delimeter);
        result.Insert(0, $"*Бой {poll.Time:t}*");
      }

      return result;
    }

    public static string GetTitle(this Poll poll, IUrlHelper urlHelper, ParseMode mode = ParseMode.Default)
    {
      var title = poll.GetTitleBase() ?? new StringBuilder();
      if (title.Length == 0)
      {
        title.Append(poll.Title);
      }

      return mode.Format(title).ToString();
    }

    public static StringBuilder GetDescription(this Poll poll, IUrlHelper urlHelper, ParseMode mode = ParseMode.Default)
    {
      var description = poll.GetTitleBase();
      if (!string.IsNullOrEmpty(poll.Title))
      {
        if (description.Length > 0)
          description.AppendLine();
        description.Append(poll.Title);
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
              .Append($"[Карта]({raid.GetLink(urlHelper)})");
          }
          break;
      }

      return description;
    }

    public static StringBuilder GetMessageText(this Poll poll, IUrlHelper urlHelper)
    {
      var text = poll.GetDescription(urlHelper, ParseMode.Markdown).AppendLine();
      
      if (poll.Cancelled)
      {
        text.AppendLine().AppendLine("*Отмена!*");
      }

      foreach (var voteGroup in (poll.Votes ?? Enumerable.Empty<Vote>())
        .GroupBy(vote => ourVoteDescription.FirstOrDefault(_ => _.Key == vote.Team).Value)
        .OrderBy(voteGroup => voteGroup.Key.Order))
      {
        var votesNumber = voteGroup.Count();
        var countStr = votesNumber == 1 ? voteGroup.Key.Singular : voteGroup.Key.Plural;
        text.AppendLine().AppendLine($"{votesNumber} {countStr}");

        foreach (var vote in voteGroup.GroupBy(_ => _.Team).OrderBy(_ => _.Key))
        {
          var votes = vote.OrderBy(v => v.Modified);
          if (votesNumber > 10) // compact mode
          {
            text
              .Append(vote.Key?.GetDescription()).Append('\x00A0')
              .AppendJoin(", ", votes.Select(v => v.User.GetLink()))
              .AppendLine();
          }
          else
          {
            text
              .AppendJoin(Environment.NewLine, votes.Select(v => $"{v.Team?.GetDescription()} {v.User.GetLink()}"))
              .AppendLine();
          }
        }
      }

      return text.Append($"[\x200B]({poll.Raid()?.GetLink(urlHelper)})");
    }

    public static InlineKeyboardMarkup GetReplyMarkup(this Poll poll)
    {
      if (poll.Cancelled)
        return null;

      var pollId = poll.Id;

      InlineKeyboardCallbackButton GetVoteButton(VoteEnum vote) =>
        new InlineKeyboardCallbackButton(vote.GetDescription(), $"vote:{pollId}:{vote}");

      return new InlineKeyboardMarkup(new[]
      {
        new InlineKeyboardButton[]
        {
          GetVoteButton(VoteEnum.Valor),
          GetVoteButton(VoteEnum.Instinct),
          GetVoteButton(VoteEnum.Mystic),
        },
        new InlineKeyboardButton[]
        {
          GetVoteButton(VoteEnum.MayBe),
          GetVoteButton(VoteEnum.Cancel),
          new InlineKeyboardSwitchInlineQueryButton("🌐", $"share:{pollId}"),
        }
      });
    }

    public static IQueryable<Poll> IncludeRelatedData(this IQueryable<Poll> polls)
    {
      return polls
        .Include(_ => _.Votes)
        .Include(_ => _.Messages)
        .Include(_ => _.Raid)
        .ThenInclude(raid => raid.PostEggRaid);
    }

    public static int? GetRaidId(this Poll poll)
    {
      return poll.Raid?.Id ?? poll.RaidId;
    }

    public static Raid Raid(this Poll poll)
    {
      return poll.Raid?.PostEggRaid ?? poll.Raid;
    }
  }
}