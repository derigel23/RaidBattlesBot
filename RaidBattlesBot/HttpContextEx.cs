using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace RaidBattlesBot
{
  public static class HttpContextEx
  {
    public static IDictionary<string, string> Properties(this HttpContext httpContext)
    {
      return httpContext?.Items?.ToDictionary(_ => Convert.ToString(_.Key), _ => Convert.ToString(_.Value))
              ?? new Dictionary<string, string>(0);
    }
  }
}