using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Geolocation;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;

namespace RaidBattlesBot
{
  public class IngressClient
  {
    private readonly HttpClient myHttpClient;
    private readonly TelemetryClient myTelemetryClient;
    private readonly RaidBattlesContext myContext;
    private readonly Location myDefaultLocation;

    public IngressClient(HttpClient httpClient, TelemetryClient telemetryClient, RaidBattlesContext context, IOptions<IngressConfiguration> options)
    {
      myHttpClient = httpClient;
      myTelemetryClient = telemetryClient;
      myContext = context;
      var configuration = options?.Value ?? throw new ArgumentNullException(nameof(options));
      myHttpClient.BaseAddress = configuration.ServiceUrl;
      myDefaultLocation = configuration.DefaultLocation ?? new Location();
    }

    public async Task<Portal> Get(string guid, Location location = default, CancellationToken cancellationToken = default)
    {
      var portalSet = myContext.Set<Portal>();
      var portal = await portalSet.FindAsync(new object[] { guid }, cancellationToken);
      if (portal == null)
      {
        var portals = await Search(guid, location ?? myDefaultLocation, cancellationToken);
        portal = portals.FirstOrDefault(p => p.Guid == guid);
        if (portal != null)
      {
          portalSet.Add(portal);
        }
      }

      return portal;
    }

    public async Task<Portal[]> Search(string query, Location location = default, CancellationToken cancellationToken = default)
    {
      location = location ?? myDefaultLocation;
      var queryBuilder = new QueryBuilder
      {
        { "lat", location.Latitude.ToString(CultureInfo.InvariantCulture) },
        { "lng", location.Longitude.ToString(CultureInfo.InvariantCulture) },
        { "query", query }
      };

      return await Execute("searchPortals.php", queryBuilder, cancellationToken);
    }

    public async Task<Portal[]> GetPortals(double radius, Location location = default, CancellationToken cancellationToken = default)
    {
      location = location ?? myDefaultLocation;
      var boundaries = new CoordinateBoundaries(location.Latitude, location.Longitude, radius, DistanceUnit.Kilometers);
      var queryBuilder = new QueryBuilder
      {
        { "nelat", boundaries.MaxLatitude.ToString(CultureInfo.InvariantCulture) },
        { "nelng", boundaries.MaxLongitude.ToString(CultureInfo.InvariantCulture) },
        { "swlat", boundaries.MinLatitude.ToString(CultureInfo.InvariantCulture) },
        { "swlng", boundaries.MinLongitude.ToString(CultureInfo.InvariantCulture) },
        { "offset", 0.ToString() }
      };

      return await Execute("getPortals.php", queryBuilder, cancellationToken, "portalData");
    }

    private async Task<Portal[]> Execute(string path, QueryBuilder parameters, CancellationToken cancellationToken = default, string property = default)
    {
      var startTime = DateTimeOffset.UtcNow;
      var timer = System.Diagnostics.Stopwatch.StartNew();
      var success = true;
      try
      {
        var response = await myHttpClient.GetAsync($"{path}{parameters}", cancellationToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
        // extract JSON from JSONP
        var resultObject = JToken.Parse(result.Substring(Math.Min(1, result.Length), Math.Max(result.Length - 2, 0)));
        var portals = (property == null ? resultObject : resultObject[property]).ToObject<Portal[]>();
        return portals ?? new Portal[0];
      }
      catch (Exception)
      {
        success = false;
        throw;
      }
      finally
      {
        timer.Stop();
        myTelemetryClient.TrackDependency("Ingress", path, parameters.ToString(), startTime, timer.Elapsed, success);
      }
    }
  }
}