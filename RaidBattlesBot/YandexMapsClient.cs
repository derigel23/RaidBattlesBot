using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RaidBattlesBot.Configuration;
using Telegram.Bot.Types;
using static System.FormattableString;

namespace RaidBattlesBot
{
  public partial class YandexMapsClient
  {
    private readonly HttpClient myHttpClient;
    private readonly TelemetryClient myTelemetryClient;
    private readonly string myKey;

    public YandexMapsClient(HttpClient httpClient, TelemetryClient telemetryClient, IOptions<GeoCoderConfiguration> options)
    {
      myHttpClient = httpClient;
      myHttpClient.BaseAddress = new Uri("https://geocode-maps.yandex.ru/1.x");
      myTelemetryClient = telemetryClient;
      myKey = options.Value?.YandexKey;
    }

    public async Task<ApiResponse> ReverseGeocode(double lat, double lng, IEnumerable<KeyValuePair<string, string>> parameters = null, CancellationToken cancellationToken = default)
    {
      var builder = new QueryBuilder(parameters ?? Enumerable.Empty<KeyValuePair<string, string>>())
      {
        { "apikey", myKey },
        { "geocode", Invariant($"{lng},{lat}") },
        { "format", "json" },
        { "lang", CultureInfo.CurrentUICulture.IetfLanguageTag }
      };

      using (var op = myTelemetryClient.StartOperation(new DependencyTelemetry(nameof(IngressClient), myHttpClient.BaseAddress.Host, "GeoCode", builder.ToString())))
      {
        var response = await myHttpClient.GetAsync(builder.ToString(), cancellationToken);
        op.Telemetry.ResultCode = response.StatusCode.ToString();
        op.Telemetry.Success = response.IsSuccessStatusCode;
        var result = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<RootObject>(result).response;
      }
    }
  }
}