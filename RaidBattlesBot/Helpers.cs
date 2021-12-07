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
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NodaTime;
using Team23.TelegramSkeleton;
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
      var assetPath = Path.Combine(assetsRoot.AbsolutePath, urlHelper.Content(contentPath));
      return new Uri(assetsRoot, assetPath);
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
    
    public static DateTimeOffset Round(this DateTimeOffset dateTime, TimeSpan interval)
    {
      var halfIntervalTicks = (interval.Ticks + 1) >> 1;

      return dateTime.AddTicks(halfIntervalTicks - ((dateTime.Ticks + halfIntervalTicks) % interval.Ticks));
    }

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
      var telemetry = new ExceptionTelemetry(exception);
      if (exception is ApiRequestException apiRequestException)
      {
        properties ??= new Dictionary<string, string>(1);
        properties["ErrorCode"] = apiRequestException.ErrorCode.ToString();
        if (exception is ApiRequestTimeoutException or ApiRequestNotFoundException)
        {
          // just warning for non-critical exceptions
          telemetry.SeverityLevel = SeverityLevel.Warning;
        }
      }
      foreach (var (key, value) in properties ?? Enumerable.Empty<KeyValuePair<string, string>>())
      {
        telemetry.Properties[key] = value;
      }

      telemetryClient.TrackException(telemetry);
    }
  }
}