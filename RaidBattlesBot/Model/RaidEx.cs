using System.Text;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Model
{
  public static class RaidEx
  {
    public static StringBuilder GetDescription(this Raid raid, ParseMode mode = ParseMode.Default)
    {
      switch (mode)
      {
        case ParseMode.Markdown:
          var descritpion = new StringBuilder();
          if (raid.RaidBossLevel.HasValue)
          {
            descritpion.Append($"[[R{raid.RaidBossLevel}]] ");
          }

          descritpion.Append($"{raid.Name}");

          return descritpion;

        default:
          return new StringBuilder(raid.Title ?? $"Raid {raid.Id}");
      }
    }
  }
}