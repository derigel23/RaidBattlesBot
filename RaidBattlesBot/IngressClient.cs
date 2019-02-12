using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Geolocation;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using NodaTime;
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
    private readonly IClock myClock;
    private readonly Location myDefaultLocation;

    public IngressClient(HttpClient httpClient, TelemetryClient telemetryClient, RaidBattlesContext context, IOptions<IngressConfiguration> options, IClock clock)
    {
      myHttpClient = httpClient;
      myTelemetryClient = telemetryClient;
      myContext = context;
      myClock = clock;
      var configuration = options?.Value ?? throw new ArgumentNullException(nameof(options));
      myHttpClient.BaseAddress = configuration.ServiceUrl;
      myHttpClient.Timeout = configuration.Timeout;
      myDefaultLocation = configuration.DefaultLocation ?? new Location();
    }

    public async Task<Portal> Get(string guid, Location location = default, CancellationToken cancellationToken = default)
    {
      if (string.IsNullOrEmpty(guid))
        return null;
      var portalSet = myContext.Set<Portal>();
      var portal = await portalSet.FindAsync(new object[] { guid }, cancellationToken);
      if ((myClock.GetCurrentInstant().ToDateTimeOffset() - portal?.Modified)?.TotalDays < 1) // refresh every day 
        return portal;
      
      var portals = await Search(guid, location ?? myDefaultLocation, cancellationToken);
      return portals.FirstOrDefault(p => p.Guid == guid);
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

    public async Task<Portal[]> Search(IReadOnlyCollection<string> query, Location location = default, CancellationToken cancellationToken = default)
    {
      var result = await Search(string.Join(' ', query), location, cancellationToken);
      if (result.Length == 0)
      {
        // skip words with length less than 3 chars
        var filteredQuery = query.Where(part => part.Length > 2).ToArray();
        if (filteredQuery.Length != query.Count)
          result = await Search(string.Join(' ', filteredQuery), location, cancellationToken);
      }      
      return result;
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
        { "offset", 0.ToString() },
        { "telegram", "" }
      };

      return await Execute("getPortals.php", queryBuilder, cancellationToken, "portalData");
    }

    private async Task<Portal[]> Execute(string path, QueryBuilder parameters, CancellationToken cancellationToken = default, string property = default)
    {
      using (var op = myTelemetryClient.StartOperation(new DependencyTelemetry(nameof(IngressClient), myHttpClient.BaseAddress.Host, path, parameters.ToString())))
      {
        var response = await myHttpClient.GetAsync($"{path}{parameters}", cancellationToken);
        op.Telemetry.ResultCode = response.StatusCode.ToString();
        op.Telemetry.Success = response.IsSuccessStatusCode;
        var result = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
        // extract JSON from JSONP
        var resultObject = JToken.Parse(result.Substring(Math.Min(1, result.Length), Math.Max(result.Length - 2, 0)));
        var portals = (property == null ? resultObject : resultObject[property]).ToObject<Portal[]>();
        return portals ?? new Portal[0];
      }
    }
  }
}