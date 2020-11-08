using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Handlers;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Model
{
  public static class RaidEx
  {
    public const string Delimeter = " ∙ ";
    
    public static StringBuilder GetDescription(this Raid raid, StringBuilder description, ParseMode mode = Helpers.DefaultParseMode)
    {
      var initialLength = description.Length;

      description.Bold((builder, m) =>
      {
        if (raid.RaidBossLevel is { } raidBossLevel)
        {
          builder.Sanitize($@"[R{raidBossLevel}] ", m);
        }

        return builder.Sanitize(raid.Name, m);
      }, mode);

      if ((raid.Gym ?? raid.PossibleGym) != null)
      {
        description
          .Sanitize(Delimeter, mode)
          .Bold((builder, m) => builder.Sanitize(raid.Gym ?? raid.PossibleGym, m), mode);
      }
      else if (raid.NearByAddress != null)
      {
        description
          .Sanitize(Delimeter, mode)
          .Bold((builder, m) => builder.Sanitize(raid.NearByAddress, m), mode);
      }

      if (description.Length == initialLength)
        description
          .Bold((builder, m) => builder.Sanitize(raid.Title, m), mode);
      
      return description;
    }

    public static Uri GetThumbUrl([CanBeNull] this Raid raid, IUrlHelper urlHelper)
    {
      var pokemonId = raid?.Pokemon;
      if (pokemonId != null)
        return urlHelper.AssetsContent($"pokemon_icons/pokemon_icon_{pokemonId:D3}_00.png");

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

    public static IQueryable<Raid> IncludeRelatedData(this IQueryable<Raid> raids) =>
      raids
        .Include(_ => _.PostEggRaid)
        .Include(_ => _.Polls)
        .ThenInclude(_ => _.Messages)
        .Include(_ => _.Polls)
        .ThenInclude(_ => _.Votes);

    public static DateTimeOffset? GetDefaultPollTime(this Raid raid) =>
      raid.RaidBossEndTime?
        .Subtract(TimeSpan.FromMinutes(15)) // default offset to the end
        .Round(TimeSpan.FromMinutes(5)); // rounding

    public static Raid ParseRaidInfo(this Raid raid, PokemonInfo pokemonInfo, string name, string moves = null)
    {
      if (name.StartsWith("EGG", StringComparison.OrdinalIgnoreCase)) // EGG
      {
        raid.Name = name.Substring(0, name.Length - 1);
        if (int.TryParse(name.Substring(name.Length - 1, 1), out var raidBossLevel))
        {
          raid.RaidBossLevel = raidBossLevel;
        }
      }
      else // BOSS
      {
        raid.Name = name;
        //raid.IV = 100; // raid bosses are always 100%
        raid.RaidBossLevel = pokemonInfo.GetRaidBossLevel(name);
        raid.Pokemon = pokemonInfo.GetPokemonNumber(name);

        if (!string.IsNullOrEmpty(moves))
        {
          InfoGymBotHelper.ProcessMoves(moves, raid);
        }
      }

      return raid;
    }

    public static Raid SetTitle(this Raid raid, StringBuilder title, bool extended = true)
    {
      raid.Title = raid.GetTitle(title, extended).ToString();
      return raid;
    }

    public static StringBuilder GetTitle(this Raid raid, StringBuilder title = null, bool extended = true)
    {
      title ??= new StringBuilder();
      if (title.Length == 0)
      {
        title
          .AppendFormat("[R{0}] ", raid.RaidBossLevel)
          .Append(raid.Name);
      }

      if (raid.EndTime is { } endTime)
      {
        title.Append($" ∙ {endTime:t}");
        if (raid.IsEgg && extended)
        {
          title.Append($"→{raid.RaidBossEndTime:t}");
        }
      }


      string GetMoveAbbreviation(string move) =>
        move.Split(' ', StringSplitOptions.RemoveEmptyEntries).Aggregate("", (agg, s) => agg + s.FirstOrDefault()).ToUpper();
      //if (raid.Move1 != null)
      //{
      //  title.Append(" ∙ ").Append(GetMoveAbbreviation(raid.Move1));
      //  if (raid.Move2 != null)
      //    title.Append('|').Append(GetMoveAbbreviation(raid.Move2));
      //}

      return title;
    }

    public static Color? GetEggColor(this Raid raid)
    {
      switch (raid.RaidBossLevel)
      {
        case 1:
        case 2:
          return Color.FromArgb(249, 125, 150  );
        case 3:
        case 4:
          return Color.FromArgb(255, 200, 35);
        case 5:
          return Color.FromArgb(22, 55, 72);
        default:
          return null;
      }
    }
    
    public static async Task<((decimal? lat, decimal? lon) location, string gym, string distance)> SetTitleAndDescription(this Raid raid, StringBuilder title, StringBuilder description, GymHelper gymHelper, int? precision = null, MidpointRounding? rounding = null, CancellationToken cancellationToken = default)
    {
      var gymInfo = await gymHelper.ProcessGym(raid.SetTitle(title), description, precision, rounding, cancellationToken);
      if (description.Length > 0)
      {
        raid.Description = description.ToString();
      }
      return gymInfo;
    }
  }
}