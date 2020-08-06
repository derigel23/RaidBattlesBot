using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NodaTime;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

namespace RaidBattlesBot
{
  public static class Helpers
  {
    #region Web

    public static Uri ForceHttps(this Uri uri)
    {
      if (uri == null)
        return null;
      
      return new UriBuilder(uri) { Scheme = "https", Port = -1 }.Uri;
    }
    
    public static Uri AssetsContent(this IUrlHelper urlHelper, [PathReference("~/PogoAssets")] string contentPath)
    {
      var assetsRoot = new Uri(urlHelper.ActionContext.HttpContext.Request.GetUri(),
        urlHelper.ActionContext.HttpContext.RequestServices.GetService<IConfiguration>()["AssetsRoot"]);
      var assetpath = Path.Combine(assetsRoot.AbsolutePath, urlHelper.Content(contentPath));
      return new Uri(assetsRoot, assetpath);
    }


    public static IHtmlContent Json(this IHtmlHelper helper, object obj) =>
      helper.Raw(JsonConvert.SerializeObject(obj));
    
    public static PageConventionCollection AddPageRouteWithName(this PageConventionCollection conventions, string pageName, string route, string name = default)
    {
      conventions.AddPageRouteModelConvention(pageName, model =>
      {
        //foreach (var selector in model.Selectors)
        //  selector.AttributeRouteModel.SuppressLinkGeneration = true;
        model.Selectors.Add(new SelectorModel
        {
          AttributeRouteModel = new AttributeRouteModel
          {
            Template = route,
            //SuppressLinkGeneration = true,
            Name = name
          }
        });
      });
      return conventions;
    }


    #endregion

    #region String

    public static string JoinNonEmpty(this string separator, params string[] values)
    {
      return string.Join(separator, values.Where(_ => !string.IsNullOrEmpty(_)));
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

    #region Formatting

    public const ParseMode DefaultParseMode = ParseMode.Html;
    
    static Helpers()
    {
      var encoderSettings = new TextEncoderSettings(UnicodeRanges.All);
      encoderSettings.ForbidCharacters('*', '_', '`');
      TelegramMarkdownEncoder = HtmlEncoder.Create(encoderSettings);
    }

    /* TODO: need better Markdown sanitizer*/
    private static readonly HtmlEncoder TelegramMarkdownEncoder;

    public static string Sanitize(this string text, ParseMode mode = DefaultParseMode) =>
      string.IsNullOrEmpty(text) ? text :
      mode == ParseMode.Markdown ? TelegramMarkdownEncoder.Encode(text) :
      mode == ParseMode.Html ? HtmlEncoder.Default.Encode(text) :
      text;

    public static readonly string NewLineString = "\n";
    
    public static StringBuilder NewLine(this StringBuilder builder) => builder.Append(NewLineString);

    public static StringBuilder Bold(this StringBuilder builder, Action<StringBuilder, ParseMode> contentBuilder, ParseMode mode = DefaultParseMode)
    {
      builder.Append(mode == ParseMode.Html ? "<b>" : mode == ParseMode.Markdown ? "*" : null);
      contentBuilder(builder, mode);
      builder.Append(mode == ParseMode.Html ? "</b>" : mode == ParseMode.Markdown ? "*" : null);
      return builder;
    }

    public static StringBuilder Code(this StringBuilder builder, Action<StringBuilder, ParseMode> contentBuilder, ParseMode mode = DefaultParseMode)
    {
      builder.Append(mode == ParseMode.Html ? "<code>" : mode == ParseMode.Markdown ? "`" : null);
      contentBuilder(builder, mode);
      builder.Append(mode == ParseMode.Html ? "</code>" : mode == ParseMode.Markdown ? "`" : null);
      return builder;
    }

    public static StringBuilder Link(this StringBuilder builder, string text, string link, ParseMode mode = DefaultParseMode)
    {
      switch (mode)
      {
        case ParseMode.Markdown when !string.IsNullOrEmpty(link):
          return builder.Append($"[{text.Sanitize(mode)}]({link})");

        case ParseMode.Html when !string.IsNullOrEmpty(link):
          return builder.Append($"<a href=\"{link}\">{text.Sanitize(mode)}</a>");

        default:
          return builder.Append(text);
      }
    }

    public static StringBuilder Sanitize(this StringBuilder builder, string content, ParseMode parseMode = DefaultParseMode) =>
      builder.Append(content.Sanitize(parseMode));
    
    public static InputTextMessageContent ToTextMessageContent(this StringBuilder builder, ParseMode parseMode = DefaultParseMode, bool disableWebPreview = false) =>
      new InputTextMessageContent(builder.ToString())
      {
        ParseMode = parseMode,
        DisableWebPagePreview = disableWebPreview
      };

    #endregion

    #region Time

    public static bool ParseTimePattern(this ZonedDateTime baseTime, string text, Regex pattern, out DateTimeOffset parsedDateTime)
    {
      parsedDateTime = DateTimeOffset.MinValue;
      return pattern.Match(text) is var match && match.Success && ParseTime(baseTime, match.Value, out parsedDateTime);
    }

    public static bool ParseTime(this ZonedDateTime baseTime, string value, out DateTimeOffset parsedDateTime)
    {
      parsedDateTime = DateTimeOffset.MinValue;
      if (DateTime.TryParseExact(value, new[] { "HH:mm:ss", "hh:mm tt", "HH:mm" }, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.NoCurrentDateDefault, out var parsedTime) ||
          DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.NoCurrentDateDefault, out parsedTime))
      {
        var parsedZonedDateTime = baseTime - Duration.FromTicks(baseTime.TickOfDay) + Duration.FromTicks(parsedTime.TimeOfDay.Ticks);
        parsedDateTime = parsedZonedDateTime.ToDateTimeOffset();
        return true;
      }

      return false;
    }

    #endregion

    public static void TrackExceptionEx(this TelemetryClient telemetryClient, Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
    {
      if (exception is AggregateException aggregateException)
      {
        aggregateException.Handle(ex => !(ex is TaskCanceledException));
      }
      if (exception is ApiRequestException apiRequestException)
      {
        properties ??= new Dictionary<string, string>(1);
        properties["ErrorCode"] = apiRequestException.ErrorCode.ToString();
      }

      telemetryClient.TrackException(exception, properties, metrics);
    }
  }
}