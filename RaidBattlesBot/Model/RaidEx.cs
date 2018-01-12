using System;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RaidBattlesBot.Model
{
  public static class RaidEx
  {
    public const string Delimeter = " ∙ ";

    public static StringBuilder GetDescription(this Raid raid)
    {
      var description = new StringBuilder();
      description.Append('*');
      if (raid.RaidBossLevel is int raidBossLevel)
      {
        description.Append($@"[R{raidBossLevel}] ");
      }

      description.Append($"{raid.Name}");
      description.Append('*');

      if ((raid.Gym ?? raid.PossibleGym) != null)
      {
        description.Append(Delimeter).Append('*').Append(raid.Gym ?? raid.PossibleGym).Append('*');
      }
      else if (raid.NearByAddress != null)
      {
        description.Append(Delimeter).Append('*').Append(raid.NearByAddress).Append('*');
      }

      if (description.Length == 0)
        description.Append('*').Append(raid.Title).Append('*');
      
      return description;
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