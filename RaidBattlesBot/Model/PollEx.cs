using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Model
{
  public static class PollEx
  {
    private static readonly Dictionary<int, (string, string)> ourVoteDescription = new Dictionary<int, (string, string)>
    {
      { 0, ("идёт", "идут") },
      { 1, ("идёт", "идут") },
      { 2, ("идёт", "идут") },
      
      { 3, ("думает", "думают") },
      
      { 4, ("передумал", "передумали") },
    };

    private static string GetVoteCounts(IGrouping<(string, string), Vote> grouping)
    {
      var number = grouping.Count();
      var countStr = number == 1 ? grouping.Key.Item1 : grouping.Key.Item2;
      return $"*{number} {countStr}:*";
    }

    public static StringBuilder GetMessageText(this Poll poll)
    {
      var text = new StringBuilder($"*{poll.Raid.Title}*").AppendLine();
      foreach (var voteGroup in (poll.Votes ?? Enumerable.Empty<Vote>())
        .GroupBy(vote => ourVoteDescription.FirstOrDefault(_ => _.Key == vote.Team).Value))
      {
        text.AppendLine().AppendLine(GetVoteCounts(voteGroup));
        foreach (var vote in voteGroup.OrderBy(_ => _.Team))
        {
          var userLink = vote.User.GetLink();
          switch (vote.Team)
          {
            case 0:
              text.AppendLine($"❤ {userLink}");
              break;
            case 1:
              text.AppendLine($"💛 {userLink}");
              break;
            case 2:
              text.AppendLine($"💙 {userLink}");
              break;
            case 3:
              text.AppendLine($"⁇ {userLink}");
              break;
            case 4:
              text.AppendLine($"✖ {userLink}");
              break;
          }
        }
      }

      text.AppendLine();
      //text.AppendLine("[link2](http://json.e2e2.ru/?lat=55.762982&lon=37.537352&b=Ninetales&t=19:20)");
      return text;
    }

    public static InlineKeyboardMarkup GetReplyMarkup(this Poll poll)
    {
      return new InlineKeyboardMarkup(new[]
      {
        new InlineKeyboardButton[]
        {
          new InlineKeyboardCallbackButton("❤", $"vote:{poll.Id}:red"),
          new InlineKeyboardCallbackButton("💛", $"vote:{poll.Id}:yellow"),
          new InlineKeyboardCallbackButton("💙", $"vote:{poll.Id}:blue"),
        },
        new InlineKeyboardButton[]
        {
          new InlineKeyboardCallbackButton("⁇", $"vote:{poll.Id}:none"),
          new InlineKeyboardCallbackButton("✖", $"vote:{poll.Id}:cancel"),
          new InlineKeyboardSwitchInlineQueryButton("🌐", $"share:{poll.Id}"),
        }
      });
    }
  }
}