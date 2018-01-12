using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Markdig;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot
{
  public static class Helpers
  {
    #region Web

    public static Uri AssetsContent(this IUrlHelper urlHelper, [PathReference("~/PogoAssets")] string contentPath)
    {
      var assetsRoot = new Uri(urlHelper.ActionContext.HttpContext.Request.GetUri(),
        urlHelper.ActionContext.HttpContext.RequestServices.GetService<IConfiguration>()["AssetsRoot"]);
      var assetpath = Path.Combine(assetsRoot.AbsolutePath, urlHelper.Content(contentPath));
      return new Uri(assetsRoot, assetpath);
    }

    #endregion

    #region String

    public static string JoinNonEmpty(this string separator, params string[] values)
    {
      return String.Join(separator, values.Where(_ => !String.IsNullOrEmpty(_)));
    }

    #endregion

    #region DateTimeOffset

    public static DateTimeOffset Floor(this DateTimeOffset dateTime, TimeSpan interval)
    {
      return dateTime.AddTicks(-(dateTime.Ticks % interval.Ticks));
    }

    public static DateTimeOffset Ceiling(this DateTimeOffset dateTime, TimeSpan interval)
    {
      var overflow = dateTime.Ticks % interval.Ticks;

      return overflow == 0 ? dateTime : dateTime.AddTicks(interval.Ticks - overflow);
    }

    public static DateTimeOffset Round(this DateTimeOffset dateTime, TimeSpan interval)
    {
      var halfIntervalTicks = (interval.Ticks + 1) >> 1;

      return dateTime.AddTicks(halfIntervalTicks - ((dateTime.Ticks + halfIntervalTicks) % interval.Ticks));
    }

    #endregion

    #region Enum

    public static string GetDescription(this Enum value)
    {
      return
        value
          .GetType()
          .GetMember(value.ToString())
          .FirstOrDefault()
          ?.GetCustomAttribute<DescriptionAttribute>()
          ?.Description;
    }

    #endregion

    #region Markdown

    public static StringBuilder Format(this ParseMode mode, StringBuilder text)
    {
      switch (mode)
      {
        case ParseMode.Markdown:
          return text;

        case ParseMode.Html:
          using (var output = new StringWriter())
          {
            Markdown.ToHtml(text.ToString(), output);
            return output.GetStringBuilder();
          }

        default:
          using (var output = new StringWriter())
          {
            Markdown.ToPlainText(text.ToString(), output);
            return output.GetStringBuilder();
          }
      }
    }

    #endregion
  }
}