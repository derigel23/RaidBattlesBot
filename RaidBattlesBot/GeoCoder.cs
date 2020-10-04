using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GoogleMapsApi;
using GoogleMapsApi.Entities.Common;
using GoogleMapsApi.Entities.DistanceMatrix.Request;
using GoogleMapsApi.Entities.Geocoding.Request;
using GoogleMapsApi.Entities.Geocoding.Response;
using GoogleMapsApi.Entities.TimeZone.Request;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;
using NodaTime;
using RaidBattlesBot.Configuration;

namespace RaidBattlesBot
{
  public class GeoCoder
  {
    private readonly TelemetryClient myTelemetryClient;
    private readonly YandexMapsClient myYandexMapsClient;
    private readonly ZonedClock myClock;
    private readonly GeoCoderConfiguration myGeoCoderOptions;

    public GeoCoder(TelemetryClient telemetryClient, IOptions<GeoCoderConfiguration> geoCoderOptions, YandexMapsClient yandexMapsClient, ZonedClock clock)
    {
      myTelemetryClient = telemetryClient;
      myYandexMapsClient = yandexMapsClient;
      myClock = clock;
      myGeoCoderOptions = geoCoderOptions.Value ?? throw new ArgumentNullException(nameof(geoCoderOptions));
    }

    public const string Delimeter = " ∙ ";

    public async Task<StringBuilder> GeoCode(Location location, StringBuilder description, CancellationToken cancellationToken = default)
    {
      try
      {
        string name = null;
        Action<StringBuilder> postProcessor = null;

        async Task<bool> CalculateDistance(string origin)
        {
          var distanceMatrixRequest = new DistanceMatrixRequest
          {
            Origins = new[] { origin },
            Destinations = new[] {location.LocationString},
            Mode = DistanceMatrixTravelModes.walking,
            ApiKey = myGeoCoderOptions.GoogleKey
          };
          var distanceMatrixResponse =
            await GoogleMaps.DistanceMatrix.QueryAsync(distanceMatrixRequest, myTelemetryClient, cancellationToken);

          var distanceElement = distanceMatrixResponse.Rows.FirstOrDefault()?.Elements.FirstOrDefault();

          if (distanceElement?.Distance.Value <= myGeoCoderOptions.MaxDistanceToMetro)
          {
            postProcessor = descr =>
              descr.Append($"{Delimeter}{distanceElement.Distance.Text}{Delimeter}{distanceElement.Duration.Text}");
            return true;
          }

          return false;
        }

        var metroResponse = await myYandexMapsClient.ReverseGeocode(location.Latitude, location.Longitude, new Dictionary<string, string>
        {
          { "kind", "metro" },
          { "results", 1.ToString() }
        }, cancellationToken);
        
        foreach (var featureMember in metroResponse.GeoObjectCollection.featureMember)
        {
          var geoObject = featureMember.GeoObject;
          if ((geoObject.Point.pos.Split(' ', 2) is { } parts) &&
              float.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var lng) &&
              float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var lat))
          {
            var origin = FormattableString.Invariant($"{lat},{lng}");
            if (await CalculateDistance(origin))
            {
              name = geoObject.name;
              break;
            }
          }
        }
//        var geoRequest = new PlacesNearByRequest
//        {
//          Location = location,
//          Type = "subway_station",
//          RankBy = RankBy.Distance,
//          ApiKey = myGeoCoderOptions.GoogleKey
//        };
//
//        var geoResponse = await GoogleMaps.PlacesNearBy.QueryAsync(geoRequest, myTelemetryClient, cancellationToken);
//
//        foreach (var address in geoResponse.Results)
//        {
//          if (address.Types.Contains("subway_station"))
//          {
//            var origin = $"place_id:{address.PlaceId}";
//            if (await CalculateDistance(origin))
//            {
//              name = address.Name;
//              break;
//            }
//          }
//        }

        if (string.IsNullOrEmpty(name))
        {
          var geoCodingRequest = new GeocodingRequest
          {
            Location = location,
            ApiKey = myGeoCoderOptions.GoogleKey
          };
          var geoCodingResults = (await GoogleMaps.Geocode.QueryAsync(geoCodingRequest, myTelemetryClient, cancellationToken)).Results;
          var result = geoCodingResults.FirstOrDefault(_ => _.Types.Contains("sublocality") || _.Types.Contains("locality"));
          name = result?.FormattedAddress;
        }

        if (!string.IsNullOrEmpty(name))
        {
          description
            .Append(description.Length > 0 ? Delimeter : "")
            .Append(name);
        }

        postProcessor?.Invoke(description);
      }
      catch (Exception ex)
      {
        myTelemetryClient.TrackExceptionEx(ex);
      }

      return description;
    }

    private class GeocodingRequestEx : GeocodingRequest
    {
      public ICollection<string> ResultType { get; set; }
      
      protected override QueryStringParametersList GetQueryStringParameters()
      {
        var parameters = base.GetQueryStringParameters();

        if (ResultType is { } types && types.Count > 0)
        {
          parameters.Add("result_type", string.Join('|', types));
        }

        return parameters;
      }
    }

    private static readonly ICollection<string> DefaultAllowedPlaceTypes = new [] { "political" };

    public async Task<(string placeId, string address, Geometry geometry)> GetPlace(string placeId, CancellationToken cancellationToken = default)
    {
      var places = await GetPlaces(placeId, cancellationToken: cancellationToken);
      return places.SingleOrDefault(_ => _.placeId == placeId);
    }
    
    public async Task<ICollection<(string placeId, string address, Geometry geometry)>> GetPlaces(string placeId, ICollection<string> allowedPlaceTypes = null, CancellationToken cancellationToken = default)
    {
      var request = new GeocodingRequestEx
      {
        PlaceId = placeId,
        ResultType = allowedPlaceTypes,
        ApiKey = myGeoCoderOptions.GoogleKey
      };

      return await ParsePlaces(request, allowedPlaceTypes ?? DefaultAllowedPlaceTypes, cancellationToken);
    }

    public async Task<ICollection<(string placeId, string address, Geometry geometry)>> GetPlaces(Location location, ICollection<string> allowedPlaceTypes = null, CancellationToken cancellationToken = default)
    {
      var request = new GeocodingRequestEx
      {
        Location = location,
        ResultType = allowedPlaceTypes,
        ApiKey = myGeoCoderOptions.GoogleKey
      };

      return await ParsePlaces(request, allowedPlaceTypes ?? DefaultAllowedPlaceTypes, cancellationToken);
    }

    private async Task<ICollection<(string placeId, string address, Geometry geometry)>> ParsePlaces(GeocodingRequest request, ICollection<string> allowedPlaceTypes, CancellationToken cancellationToken = default)
    {
      var response = await GoogleMaps.Geocode.QueryAsync(request, myTelemetryClient, cancellationToken);
      var places = new List<(string placeId, string address, Geometry geometry)>();
      foreach (var result in response.Results)
      {
        foreach (var type in result.Types)
        {
          if ((allowedPlaceTypes == null) || allowedPlaceTypes.Contains(type))
          {
            places.Add((result.PlaceId, result.FormattedAddress, result.Geometry));
            break;
          }
        }
      }

      return places;
    }

    public async Task<DateTimeZone> GetTimeZone(Location location, CancellationToken cancellationToken = default)
    {
      var timeZoneRequest = new TimeZoneRequest
      {
        Location = location,
        TimeStamp = myClock.GetCurrentInstant().ToDateTimeUtc(),
        ApiKey = myGeoCoderOptions.GoogleKey
      };
      var timeZoneResponse = await GoogleMaps.TimeZone.QueryAsync(timeZoneRequest, myTelemetryClient, cancellationToken);
      
      if (!timeZoneResponse.IsSuccess())
        return myClock.GetCurrentZonedDateTime().Zone;

      if (timeZoneResponse.TimeZoneId is { } timeZoneId &&
          DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId) is { } dateTimeZone)
        return dateTimeZone;

      return DateTimeZone.ForOffset(Offset.FromSeconds((int) (timeZoneResponse.RawOffSet + timeZoneResponse.DstOffSet)));
    }
  }
}