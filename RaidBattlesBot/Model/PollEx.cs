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
    private static readonly Dictionary<VoteEnum, (string, string)> ourVoteDescription = new Dictionary<VoteEnum, (string, string)>
    {
      { VoteEnum.Valor, ("идёт", "идут") },
      { VoteEnum.Instinct, ("идёт", "идут") },
      { VoteEnum.Mystic, ("идёт", "идут") },
      
      { VoteEnum.MayBe, ("думает", "думают") },
      
      { VoteEnum.Cancel, ("передумал", "передумали") },
    };

    private static string GetVoteCounts(IGrouping<(string, string), Vote> grouping)
    {
      var number = grouping.Count();
      var countStr = number == 1 ? grouping.Key.Item1 : grouping.Key.Item2;
      return $"{number} {countStr}:";
    }

    public static Uri GetThumbUrl(this Poll poll, IUrlHelper urlHelper)
    {
      return  poll.Raid().GetThumbUrl(urlHelper);
    }

    private static StringBuilder GetTitleBase(this Poll poll, IUrlHelper urlHelper, ParseMode mode = ParseMode.Default)
    {
      var result = poll.Raid()?.GetDescription(urlHelper, mode) ?? new StringBuilder();
      if (poll.Time != null)
      {
        var insertPos = result.ToString().IndexOf(RaidEx.Delimeter, StringComparison.Ordinal) is var pos && pos >= 0 ? pos : result.Length;
        result.Insert(insertPos, $"{RaidEx.Delimeter}{poll.Time:t}");
      }

      return result;
    }

    public static string GetTitle(this Poll poll, IUrlHelper urlHelper, ParseMode mode = ParseMode.Default)
    {
      var title = poll.GetTitleBase(urlHelper, mode);
      if (title.Length == 0)
      {
        return poll.Title;
      }

      return title.ToString();
    }

    public static StringBuilder GetDescription(this Poll poll, IUrlHelper urlHelper, ParseMode mode = ParseMode.Default)
    {
      var description = poll.GetTitleBase(urlHelper, mode);
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
              .Append($"[Карта]({urlHelper.Page("/Raid", null, new { raidId = raid.Id }, protocol: "https")})");
          }
          break;
      }

      return description;
    }

    public static string GetMessageText(this Poll poll, IUrlHelper urlHelper)
    {
      var text = poll.GetDescription(urlHelper, ParseMode.Markdown).AppendLine();
      
      if (poll.Cancelled)
      {
        text.AppendLine().AppendLine("*Отмена!*");
      }

      foreach (var voteGroup in (poll.Votes ?? Enumerable.Empty<Vote>())
        .GroupBy(vote => ourVoteDescription.FirstOrDefault(_ => _.Key == vote.Team).Value))
      {
        text.AppendLine().AppendLine(GetVoteCounts(voteGroup));
        foreach (var vote in voteGroup.OrderBy(_ => _.Team))
        {
          var userLink = vote.User.GetLink();
          switch (vote.Team)
          {
            case VoteEnum.Valor:
              text.AppendLine($"❤ {userLink}");
              break;
            case VoteEnum.Instinct:
              text.AppendLine($"💛 {userLink}");
              break;
            case VoteEnum.Mystic:
              text.AppendLine($"💙 {userLink}");
              break;
            case VoteEnum.MayBe:
              text.AppendLine($"⁇ {userLink}");
              break;
            case VoteEnum.Cancel:
              text.AppendLine($"✖ {userLink}");
              break;
          }
        }
      }

      return text.Append($"[\x200B]({urlHelper.Page("/Raid", null, new { raidId = poll.Raid()?.Id }, protocol: "https")})").ToString();
    }

    public static InlineKeyboardMarkup GetReplyMarkup(this Poll poll)
    {
      if (poll.Cancelled)
        return null;

      var pollId = poll.Id;

      return new InlineKeyboardMarkup(new[]
      {
        new InlineKeyboardButton[]
        {
          new InlineKeyboardCallbackButton("❤", $"vote:{pollId}:red"),
          new InlineKeyboardCallbackButton("💛", $"vote:{pollId}:yellow"),
          new InlineKeyboardCallbackButton("💙", $"vote:{pollId}:blue"),
        },
        new InlineKeyboardButton[]
        {
          new InlineKeyboardCallbackButton("⁇", $"vote:{pollId}:none"),
          new InlineKeyboardCallbackButton("✖", $"vote:{pollId}:cancel"),
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

    public static Raid Raid(this Poll poll)
    {
      return poll.Raid?.PostEggRaid ?? poll.Raid;
    }
  }
}