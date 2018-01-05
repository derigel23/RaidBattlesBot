using System.Linq;

namespace RaidBattlesBot
{
  public static class StringEx
  {
    public static string JoinNonEmpty(string separator, params string[] values)
    {
      return string.Join(separator, values.Where(_ => !string.IsNullOrEmpty(_)));
    }
  }
}