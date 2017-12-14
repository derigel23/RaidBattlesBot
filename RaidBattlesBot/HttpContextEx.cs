using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace RaidBattlesBot
{
  public static class HttpContextEx
  {
    public static IDictionary<string, string> Properties(this HttpContext httpContext)
    {
      return httpContext.Items.ToDictionary(_ => _.Key.ToString(), _ => _.Value.ToString());
    }
  }
}