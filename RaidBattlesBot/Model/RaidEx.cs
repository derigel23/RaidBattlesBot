using System;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Model
{
  public static class RaidEx
  {
    public const string Delimeter = " ∙ ";

    public static StringBuilder GetDescription(this Raid raid, IUrlHelper urlHelper, ParseMode mode = ParseMode.Default)
    {
      var description = new StringBuilder("*");
      if (raid.RaidBossLevel is int raidBossLevel)
      {
        description.Append($@"[R{raidBossLevel}] ");
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

      if (description.Length == 0)
        description.Append(raid.Title);

      description.Append("*");
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

    public static Uri GetThumbUrl([CanBeNull] this Raid raid, IUrlHelper urlHelper)
    {
      var pokemonId = raid?.Pokemon;
      if (pokemonId != null)
        return urlHelper.AssetsContent($"decrypted_assets/pokemon_icon_{pokemonId:D3}_00.png");

      var raidRaidBossLevel = raid?.RaidBossLevel;
      switch (raidRaidBossLevel)
      {
        case 1:
        case 2:
          return urlHelper.AssetsContent("static_assets/png/ic_raid_egg_normal.png");
        case 3:
        case 4:
          return urlHelper.AssetsContent("static_assets/png/ic_raid_egg_rare.png");
        case 5:
          return urlHelper.AssetsContent("static_assets/png/ic_raid_egg_legendary.png");
      }

      return urlHelper.AssetsContent("static_assets/png/raid_tut_raid.png");
    }

    public static string GetLink(this Raid raid, IUrlHelper urlHelper)
    {
      return urlHelper.Page("/Raid", null, new { raidId = raid.Id }, protocol: "https");
    }

    public static IQueryable<Raid> IncludeRelatedData(this IQueryable<Raid> raids)
    {
      return raids
        .Include(_ => _.PostEggRaid)
        .Include(_ => _.Polls)
        .ThenInclude(_ => _.Messages)
        .Include(_ => _.Polls)
        .ThenInclude(_ => _.Votes);
    }
  }
}