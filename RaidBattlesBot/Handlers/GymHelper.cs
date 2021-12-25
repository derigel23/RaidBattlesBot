using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GoogleMapsApi;
using GoogleMapsApi.Entities.Common;
using GoogleMapsApi.Entities.DistanceMatrix.Request;
using GoogleMapsApi.Entities.Geocoding.Request;
using GoogleMapsApi.Entities.PlacesNearBy.Request;
using GoogleMapsApi.Entities.PlacesNearBy.Response;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;

namespace RaidBattlesBot.Handlers
{
  public class GymHelper
  {
    private readonly RaidBattlesContext myDbContext;
    private readonly TelemetryClient myTelemetryClient;
    private readonly GeoCoderConfiguration myGeoCoderOptions;

    public GymHelper(RaidBattlesContext dbContext, TelemetryClient telemetryClient, IOptions<GeoCoderConfiguration> geoCoderOptions)
    {
      myDbContext = dbContext;
      myTelemetryClient = telemetryClient;
      myGeoCoderOptions = geoCoderOptions.Value ?? throw new ArgumentNullException(nameof(geoCoderOptions));
    }

    public const int LowerDecimalPrecision = 4;
    public const MidpointRounding LowerDecimalPrecisionRounding = default;

    public async Task<((decimal? lat, decimal? lon) location, string gym, string distance)> ProcessGym(Raid raid, TextBuilder description, int? precision = null, MidpointRounding? rounding = null, CancellationToken cancellationToken = default)
    {
      var location = (lat: raid.Lat, lon: raid.Lon);
      string distance = default;
      var gym = raid.Gym ?? raid.PossibleGym;
      if (gym == null)
      {
        raid.PossibleGym = await myDbContext.Set<Raid>()
          .FindKnownGym((decimal) raid.Lat, (decimal) raid.Lon, precision, rounding)
          .Select(_ => _.Gym ?? _.PossibleGym)
          .FirstOrDefaultAsync(cancellationToken);
      }

      //if (!string.IsNullOrEmpty(gym))
      //{
      //  description.Append(gym);
      //}

      //var geoCode = await myGeoCoder.Decode(lon, lat, cancellationToken: cancellationToken,
      //    parameters: new Dictionary<string, string>
      //    {
      //        {"results", "1"},
      //        {"kind", "metro"}
      //    });
      //var metro = geoCode?.featureMember?.FirstOrDefault()?.GeoObject;

      try
      {
        var destination = new Location((double)location.lat, (double)location.lon);
        var geoRequest = new PlacesNearByRequest
        {
          Location = destination,
          Type = "subway_station",
          RankBy = RankBy.Distance,
          ApiKey = myGeoCoderOptions.GoogleKey
        };

        var geoResponse = await GoogleMaps.PlacesNearBy.QueryAsync(geoRequest, myTelemetryClient, cancellationToken);

        Result foundAddress = null;
        Action<TextBuilder> postProcessor = null;
        foreach (var address in geoResponse.Results)
        {
          if (address.Types.Contains("subway_station"))
          {
            var distanceMatrixRequest = new DistanceMatrixRequest
            {
              Origins = new[] {$"place_id:{address.PlaceId}"},
              Destinations = new[] {destination.LocationString},
              Mode = DistanceMatrixTravelModes.walking,
              ApiKey = myGeoCoderOptions.GoogleKey
            };
            var distanceMatrixResponse = await GoogleMaps.DistanceMatrix.QueryAsync(distanceMatrixRequest, myTelemetryClient, cancellationToken);

            var distanceElement = distanceMatrixResponse.Rows.FirstOrDefault()?.Elements.FirstOrDefault();

            if (distanceElement?.Distance.Value <= myGeoCoderOptions.MaxDistanceToMetro)
            {
              foundAddress = address;
              postProcessor = descr => descr.Sanitize(distance = $" ∙ {distanceElement.Distance.Text} ∙ {distanceElement.Duration.Text}");
              break;
            }
          }
        }

        string placeId;
        string name;
        if (foundAddress == null)
        {
          var geoCodingRequest = new GeocodingRequest
          {
            Location = destination,
            ApiKey = myGeoCoderOptions.GoogleKey
          };
          var geoCodingResults = (await GoogleMaps.Geocode.QueryAsync(geoCodingRequest, myTelemetryClient, cancellationToken)).Results;
          var result = geoCodingResults.FirstOrDefault(_ => _.Types.Contains("locality"));
          placeId = result?.PlaceId;
          name = result?.FormattedAddress;
        }
        else
        {
          placeId = foundAddress.PlaceId;
          name = foundAddress.Name;
        }

        raid.NearByPlaceId = placeId;
        raid.NearByAddress = name;

        if (!string.IsNullOrEmpty(name))
        {
          description
            .Sanitize(description.Length > 0 ? " ∙ " : "")
            .Sanitize(name);
          
          postProcessor?.Invoke(description);
        }
      }
      catch (Exception ex)
      {
        myTelemetryClient.TrackExceptionEx(ex);
      }

      return (location, gym, distance);
    }
  }
}