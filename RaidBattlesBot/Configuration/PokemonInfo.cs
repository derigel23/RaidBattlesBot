using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RaidBattlesBot.Configuration
{
  public class PokemonInfo
  {
    public IReadOnlyDictionary<string, int> Names { get; }
    public IReadOnlyDictionary<int, int> Raids { get; }

    public PokemonInfo(IConfigurationRoot namesConfig, IConfigurationRoot raidsConfig, ILoggerFactory loggerFactory)
    {
      var logger = loggerFactory.CreateLogger<PokemonInfo>();

      var knownPokemonNumbers = new HashSet<int>();
      var names = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
      foreach (var provider in namesConfig.Providers)
      {
        foreach (var key in provider.GetChildKeys(Enumerable.Empty<string>(), null))
        {
          if (provider.TryGet(key, out var name) && int.TryParse(key, out var number))
          {
            name = System.Text.RegularExpressions.Regex.Unescape(name);
            if (names.TryAdd(name, number))
            {
              knownPokemonNumbers.Add(number);
            }
            else
            {
              if (names[name] != number)
              {
                logger.LogError("Duplicate pokemon name #{0} {1}", number, name);
              }
            }
          }
        }
      }
      Names = names;

      var raids = new Dictionary<int, int>();
      foreach (var pair in raidsConfig.AsEnumerable())
      {
        var val = pair.Value;
        if (string.IsNullOrEmpty(val)) continue;
        
        val = (val.IndexOf('#') is var pos && pos >= 0) ? val.Substring(0, pos).TrimEnd() : val;

        if (int.TryParse(pair.Key, out var pokemonNumber) && int.TryParse(val, out var raidLevel))
        {
          if (knownPokemonNumbers.Contains(pokemonNumber))
          {
            if (!raids.TryAdd(pokemonNumber, raidLevel))
            {
              logger.LogError("Duplicate pokemon raid info {0}", pair);
            }
          }
          else
          {
            logger.LogError("Unknown pokemon raid info {0}", pair);
          }
        }
        else
        {
          logger.LogError("Wrong raid boss info {0}", pair);
        }
      }
      Raids = raids;
    }
  }
}