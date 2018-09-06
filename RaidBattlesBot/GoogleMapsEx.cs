using System;
using System.Threading;
using System.Threading.Tasks;
using GoogleMapsApi;
using GoogleMapsApi.Entities.Common;
using GoogleMapsApi.Entities.PlacesNearBy.Request;
using GoogleMapsApi.Entities.PlacesNearBy.Response;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace RaidBattlesBot
{
  public static class GoogleMapsEx
  {
    public static async Task<TResponse> QueryAsync<TRequest, TResponse>(this IEngineFacade<TRequest, TResponse> engine, TRequest request, TelemetryClient telemetryClient, Action<DependencyTelemetry, TResponse> postProcessor = null, CancellationToken cancellationToken = default)
      where TRequest : MapsBaseRequest, new() where TResponse : PlacesNearByResponse, IResponseFor<TRequest>
    {
      var uri = request.GetUri();
      using (var op = telemetryClient.StartOperation(new DependencyTelemetry(nameof(GoogleMaps), uri.Host, engine.ToString(), uri.ToString())))
      {
        var response = await engine.QueryAsync(request, cancellationToken);
        postProcessor?.Invoke(op.Telemetry, response);
        return response;
      }
    }
    
    public static async Task<PlacesNearByResponse> QueryAsync(this IEngineFacade<PlacesNearByRequest, PlacesNearByResponse> engine, PlacesNearByRequest request, TelemetryClient telemetryClient, CancellationToken cancellationToken = default)
    {
      return await QueryAsync(engine, request, telemetryClient, (telemetry, response) =>
      {
        telemetry.Name = nameof(GoogleMaps.PlacesNearBy);
        telemetry.Success = response.Status == Status.OK || response.Status == Status.ZERO_RESULTS;
        telemetry.ResultCode = response.Status.ToString();
      }, cancellationToken);
    }
  }

}