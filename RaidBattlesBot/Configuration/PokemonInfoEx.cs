namespace RaidBattlesBot.Configuration
{
  public static class PokemonInfoEx
  {
    public static int? GetPokemonNumber(this PokemonInfo pokemons, string pokemonName) =>
      string.IsNullOrEmpty(pokemonName) ? default :
        pokemons.Names.TryGetValue(pokemonName, out var pokemonNumber) ? pokemonNumber : default(int?);

    public static int? GetRaidBossLevel(this PokemonInfo pokemons, string pokemonName) =>
      pokemons.Raids.TryGetValue(pokemons.GetPokemonNumber(pokemonName).GetValueOrDefault(), out var raidBossLevel) ? raidBossLevel : default(int?);

  }
}