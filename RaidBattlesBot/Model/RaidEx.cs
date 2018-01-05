using System;
using System.IO;
using System.Text;
using Markdig;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Model
{
  public static class RaidEx
  {
    public const string Delimeter = " ∙ ";

    public static StringBuilder GetDescription(this Raid raid, ParseMode mode = ParseMode.Default)
    {
      var description = new StringBuilder();
      if (raid.RaidBossLevel.HasValue)
      {
        description.Append($@"\[R{raid.RaidBossLevel}] ");
      }

      description.Append($"{raid.Name}");

      if ((raid.Gym ?? raid.PossibleGym) != null)
      {
        description.Append(Delimeter).Append(raid.Gym ?? raid.PossibleGym);
      }
      else if (raid.NearByAddress != null)
      {
        description.Append(Delimeter).Append(raid.NearByAddress);
      }

      switch (mode)
      {
        case ParseMode.Markdown:
          return description;

        case ParseMode.Html:
          using (var output = new StringWriter())
          {
            Markdown.ToHtml(description.ToString(), output);
            return output.GetStringBuilder();
          }

        default:
          using (var output = new StringWriter())
          {
            Markdown.ToPlainText(description.ToString(), output);
            return output.GetStringBuilder();
          }
      }
    }

    public static DateTimeOffset? RaidBossEndTime(this Raid raid)
    {
      if ((raid.RaidBossLevel != null) && (raid.Pokemon == null)) // egg
      {
        return raid.EndTime?.Add(TimeSpan.FromMinutes(45)); // boss lifetime
      }

      return raid.EndTime;
    }
  }
}