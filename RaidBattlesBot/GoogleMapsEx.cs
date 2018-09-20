using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using GoogleMapsApi;
using GoogleMapsApi.Entities.Common;
using GoogleMapsApi.Entities.DistanceMatrix.Request;
using GoogleMapsApi.Entities.DistanceMatrix.Response;
using GoogleMapsApi.Entities.Geocoding.Request;
using GoogleMapsApi.Entities.Geocoding.Response;
using GoogleMapsApi.Entities.PlacesNearBy.Request;
using GoogleMapsApi.Entities.PlacesNearBy.Response;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Status = GoogleMapsApi.Entities.PlacesNearBy.Response.Status;

namespace RaidBattlesBot
{
  public static class GoogleMapsEx
  {
    public static async Task<TResponse> QueryAsync<TRequest, TResponse>(this IEngineFacade<TRequest, TResponse> engine, TRequest request, TelemetryClient telemetryClient, Action<DependencyTelemetry, TResponse> postProcessor = null, CancellationToken cancellationToken = default)
      where TRequest : MapsBaseRequest, new() where TResponse : IResponseFor<TRequest>
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
      request.Language = CultureInfo.CurrentUICulture.IetfLanguageTag;
      return await QueryAsync(engine, request, telemetryClient, (telemetry, response) =>
      {
        telemetry.Name = nameof(GoogleMaps.PlacesNearBy);
        switch (response.Status)
        {
          case Status.OK:
          case Status.ZERO_RESULTS:
            telemetry.Success = true;
            break;
          default:
            telemetry.Success = false;
            break;
        }
        telemetry.ResultCode = response.Status.ToString();
      }, cancellationToken);
    }
    
    public static async Task<DistanceMatrixResponse> QueryAsync(this IEngineFacade<DistanceMatrixRequest, DistanceMatrixResponse> engine, DistanceMatrixRequest request, TelemetryClient telemetryClient, CancellationToken cancellationToken = default)
    {
      request.Language = CultureInfo.CurrentUICulture.IetfLanguageTag;
      return await QueryAsync(engine, request, telemetryClient, (telemetry, response) =>
      {
        telemetry.Name = nameof(GoogleMaps.DistanceMatrix);
        switch (response.Status)
        {
          case DistanceMatrixStatusCodes.OK:
          case DistanceMatrixStatusCodes.NOT_FOUND:
          case DistanceMatrixStatusCodes.ZERO_RESULTS:
            telemetry.Success = true;
            break;
          default:
            telemetry.Success = false;
            break;
        }
        telemetry.ResultCode = response.Status.ToString();
      }, cancellationToken);
    }
    
    public static async Task<GeocodingResponse> QueryAsync(this IEngineFacade<GeocodingRequest, GeocodingResponse> engine, GeocodingRequest request, TelemetryClient telemetryClient, CancellationToken cancellationToken = default)
    {
      request.Language = CultureInfo.CurrentUICulture.IetfLanguageTag;
      return await QueryAsync(engine, request, telemetryClient, (telemetry, response) =>
      {
        telemetry.Name = nameof(GoogleMaps.Geocode);
        switch (response.Status)
        {
          case GoogleMapsApi.Entities.Geocoding.Response.Status.OK:
          case GoogleMapsApi.Entities.Geocoding.Response.Status.ZERO_RESULTS:
            telemetry.Success = true;
            break;
          default:
            telemetry.Success = false;
            break;
        }
        telemetry.ResultCode = response.Status.ToString();
      }, cancellationToken);
    }
  }

}