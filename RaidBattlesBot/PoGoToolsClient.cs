using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http.Extensions;

namespace RaidBattlesBot
{
  public class PoGoToolsClient
  {
    private readonly HttpClient myHttpClient;
    private readonly TelemetryClient myTelemetryClient;

    public PoGoToolsClient(HttpClient httpClient, TelemetryClient telemetryClient)
    {
      myHttpClient = httpClient;
      myTelemetryClient = telemetryClient;
      myHttpClient.BaseAddress = new Uri("https://pogo.tools");
    }

    public async Task UpdateWayspot(string guid, Wayspot type, CancellationToken cancellationToken = default)
    {
      var parameters = new QueryBuilder
      {
        {"id", guid},
        {"type", Enums.Format(type, EnumFormat.UnderlyingValue)}
      };

      const string importWayspot = "import/wayspot";
      using (var op = myTelemetryClient.StartOperation(new DependencyTelemetry(nameof(PoGoToolsClient),myHttpClient.BaseAddress.Host, importWayspot, parameters.ToString())))
      {
        var responseMessage = await myHttpClient.PostAsync(importWayspot, new FormUrlEncodedContent(parameters), cancellationToken);
        op.Telemetry.ResultCode = responseMessage.StatusCode.ToString();
        op.Telemetry.Success = responseMessage.IsSuccessStatusCode;
      }
    }
  }
  
  public enum Wayspot : byte
  {
    Pokestop = 1,
    Gym = 2,
    ExRaidGym = 4,
    Other = 128,
  }
}