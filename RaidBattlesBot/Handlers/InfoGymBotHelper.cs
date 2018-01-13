using System;
using System.Text.RegularExpressions;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  /// <summary>
  /// Handle messages and venues from @InfoGymBot and @RaidInfoBot
  /// </summary>
  public class InfoGymBotHelper
  {
    private readonly PokemonInfo myPokemonInfo;

    public InfoGymBotHelper(PokemonInfo pokemonInfo)
    {
      myPokemonInfo = pokemonInfo;
    }

    public static bool IsAppropriateUrl(Uri requestUri) => requestUri.Host == "json.e2e2.ru";

    public static bool IsAppropriateMessage(Message message)
    {
      switch ((message.ForwardFrom ?? message.From)?.Username)
      {
        case "infogymbot":
        case "raidinfobot":
          return true;
          
        default:
          return false;
      }
    }

    private static readonly Regex VenueTitleParser = new Regex("R:(?<title>.+) до (?<time>\\d+:\\d+)");

    public bool ProcessVenue(Venue venue, Raid raid)
    {
      if (VenueTitleParser.Match(venue.Title) is var match && match.Success)
      {
        var pokemon = match.Groups["title"].Value;
        raid.Pokemon = myPokemonInfo.GetPokemonNumber(pokemon);
        raid.Name = pokemon;
        raid.RaidBossLevel = myPokemonInfo.GetRaidBossLevel(pokemon);

        ProcessMoves(venue.Address, raid);

        return true;
      }

      return false;
    }

    public static void ProcessMoves(string movesString, Raid raid)
    {
      var moves = movesString.TrimEnd().Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
      string GetMove(int i) => (moves.Length > i ? moves[i] : null) is string move ? string.IsNullOrEmpty(move) ? null : move : null;
      raid.Move1 = GetMove(0);
      raid.Move2 = GetMove(1);
    }
  }
}