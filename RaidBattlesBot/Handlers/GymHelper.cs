using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GoogleMapsApi;
using GoogleMapsApi.Entities.Common;
using GoogleMapsApi.Entities.DistanceMatrix.Request;
using GoogleMapsApi.Entities.PlacesNearBy.Request;
using GoogleMapsApi.Entities.PlacesNearBy.Response;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;

namespace RaidBattlesBot.Handlers
{
  public class GymHelper
  {
    private readonly RaidBattlesContext myDbContext;
    private readonly Gyms myGyms;
    private readonly TelemetryClient myTelemetryClient;
    private readonly IHttpContextAccessor myHttpContextAccessor;
    private readonly IOptions<GeoCoderConfiguration> myGeoCoderOptions;

    public GymHelper(RaidBattlesContext dbContext, Gyms gyms, TelemetryClient telemetryClient, IHttpContextAccessor httpContextAccessor, IOptions<GeoCoderConfiguration> geoCoderOptions)
    {
      myDbContext = dbContext;
      myGyms = gyms;
      myTelemetryClient = telemetryClient;
      myHttpContextAccessor = httpContextAccessor;
      myGeoCoderOptions = geoCoderOptions;
    }

    public async Task<(string gym, string park, string distance)> ProcessGym(Raid raid, StringBuilder description, int? precision = null, CancellationToken cancellationToken = default(CancellationToken))
    {
      var park = default(string);
      var distance = default(string);
      var gym = raid.Gym ?? raid.PossibleGym ??
            (raid.PossibleGym = await myDbContext.Raids
              .FindKnownGym((decimal)raid.Lat, (decimal)raid.Lon, precision)
              .Select(_ => _.Gym ?? _.PossibleGym)
              .FirstOrDefaultAsync(cancellationToken));
      if (myGyms.TryGet((decimal)raid.Lat, (decimal)raid.Lon, out var foundGymInfo, precision))
      {
        gym = gym ?? (raid.PossibleGym = foundGymInfo.gym);
        gym = "★ " + gym;
        myHttpContextAccessor.HttpContext.Items["possibleMewto"] = true;
        myHttpContextAccessor.HttpContext.Items["park"] = park = foundGymInfo.park;
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
        var destination = new Location((double)raid.Lat, (double)raid.Lon);
        var geoRequest = InitGeoRequest(new PlacesNearByRequest
        {
          Location = destination,
          Type = "subway_station",
          RankBy = RankBy.Distance,
        });

        var geoResponse = await GoogleMaps.PlacesNearBy.QueryAsync(geoRequest, cancellationToken);
        if (geoResponse.Status == Status.ZERO_RESULTS)
        {
          geoRequest.Type = "locality";
          geoResponse = await GoogleMaps.PlacesNearBy.QueryAsync(geoRequest, cancellationToken);
        }
        var address = geoResponse.Results.FirstOrDefault();
        
        if (address != null)
        {
          raid.NearByPlaceId = address.PlaceId;
          raid.NearByAddress = address.Name;
          description
            .Append(description.Length > 0 ? " ∙ " : "")
            .Append(address.Name);

          if (address.Types.Contains("subway_station"))
          {
            var distanceMatrixRequest = InitGeoRequest(new DistanceMatrixRequest
            {
              Origins = new[] {$"place_id:{address.PlaceId}"},
              Destinations = new[] {destination.LocationString},
              Mode = DistanceMatrixTravelModes.walking,
            });
            var distanceMatrixResponse = await GoogleMaps.DistanceMatrix.QueryAsync(distanceMatrixRequest, cancellationToken);

            var distanceElement = distanceMatrixResponse.Rows.FirstOrDefault()?.Elements.FirstOrDefault();

            if (distanceElement != null)
            {
              description.Append(distance = $" ∙ {distanceElement.Distance.Text} ∙ {distanceElement.Duration.Text}");
            }
          }
        }
      }
      catch (Exception ex)
      {
        myTelemetryClient.TrackException(ex, myHttpContextAccessor.HttpContext.Properties());
      }

      return (gym, park, distance);
    }

    private TRequest InitGeoRequest<TRequest>(TRequest request)
      where TRequest : MapsBaseRequest
    {
      request.ApiKey = myGeoCoderOptions.Value?.GoogleKey;
      if (request is ILocalizableRequest loc)
      {
        loc.Language = CultureInfo.CurrentCulture.Name;
      }
      return request;
    }
  }
}