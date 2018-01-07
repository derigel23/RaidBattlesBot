namespace RaidBattlesBot.Configuration
{
  public static class PokemonInfoEx
  {
    public static int? GetPokemonNumber(this PokemonInfo pokemons, string pokemonName) =>
      string.IsNullOrEmpty(pokemonName) ? null :
        pokemons.Names.TryGetValue(pokemonName, out var pokemonNumber) ? pokemonNumber : default(int?);

    public static int? GetRaidBossLevel(this PokemonInfo pokemons, string pokemonName) =>
      pokemons.GetPokemonNumber(pokemonName) is int pokemonNumber ?
        pokemons.Raids.TryGetValue(pokemonNumber, out var raidBossLevel) ? raidBossLevel : default(int?) : null;

  }
}