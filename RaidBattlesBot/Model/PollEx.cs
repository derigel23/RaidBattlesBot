using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public static string GetTitle(this Poll poll)
    {
      return poll.Title ?? poll.Raid?.Title;
    }

    public static string GetMessageText(this Poll poll)
    {
      var text = new StringBuilder($"{poll.GetTitle()}").AppendLine();
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

      text.AppendLine();
      //text.AppendLine("[link2](http://json.e2e2.ru/?lat=55.762982&lon=37.537352&b=Ninetales&t=19:20)");
      return text.ToString();
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
  }
}