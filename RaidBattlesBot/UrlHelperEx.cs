using System;
using System.IO;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RaidBattlesBot
{
  public static class UrlHelperEx
  {
    public static Uri AssetsContent(this IUrlHelper urlHelper, [PathReference("~/PogoAssets")] string contentPath)
    {
      var assetsRoot = new Uri(urlHelper.ActionContext.HttpContext.Request.GetUri(),
        urlHelper.ActionContext.HttpContext.RequestServices.GetService<IConfiguration>()["AssetsRoot"]);
      var assetpath = Path.Combine(assetsRoot.AbsolutePath, urlHelper.Content(contentPath));
      return new Uri(assetsRoot, assetpath);
    }
  }
}