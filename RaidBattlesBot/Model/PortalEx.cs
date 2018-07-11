using System;
using Microsoft.AspNetCore.Mvc;

namespace RaidBattlesBot.Model
{
  public static class PortalEx
  {
    public static Uri GetImage(this Portal portal, IUrlHelper urlHelper, int? thumbnail = null, bool fallbackToDefault = true)
    {
      if (portal?.Image is string image && !string.IsNullOrEmpty(image))
      {
        var imageUrl = new Uri(image);
        return thumbnail is int size ?
          new UriBuilder(image) { Path = $"{imageUrl.AbsolutePath}=s{size}-c" }.Uri : imageUrl;
      }

      return fallbackToDefault ? urlHelper.AssetsContent("static_assets/png/btn_pokestop.png") : null;
    }
  }
}