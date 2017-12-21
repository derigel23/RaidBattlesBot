using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RaidBattlesBot.Model;

// ReSharper disable PossibleNullReferenceException

namespace RaidBattlesBot.Configuration
{
  public class Gyms
  {
    public const int LowerDecimalPrecision = 4;
      
    private readonly IDictionary<(decimal lat, decimal lon), (string gym, string park)> myGymInfo = new ConcurrentDictionary<(decimal lat, decimal lon), (string gym, string park)>();
    private readonly IDictionary<(decimal lat, decimal lon), (string gym, string park)> myGymLowerPrecisionInfo = new ConcurrentDictionary<(decimal lat, decimal lon), (string gym, string park)>();
    
    public Gyms(Stream stream)
    {
      var ns = XNamespace.Get("http://www.opengis.net/kml/2.2");
      foreach (var placemark in XDocument.Load(stream)
        .Element(ns + "kml")
        .Element(ns + "Document")?
        .Elements(ns + "Placemark"))
      {
        var extendedData = placemark
          .Element(ns + "ExtendedData")
          .Elements(ns + "Data")
          .ToDictionary(_ => _.Attribute("name").Value, _=> _.Element(ns + "value").Value);
        var name = placemark.Element(ns + "name").Value;
        if (decimal.TryParse(extendedData["lat"], NumberStyles.Currency, CultureInfo.InvariantCulture, out var lat) &&
            decimal.TryParse(extendedData["lng"], NumberStyles.Currency, CultureInfo.InvariantCulture, out var lon))
        {
          var data = (name, extendedData.GetValueOrDefault("park_name"));
          myGymInfo.Add((lat, lon), data);
          myGymLowerPrecisionInfo[RaidHelpers.LowerPrecision(lat, lon, LowerDecimalPrecision)] = data;
        }
      }
    }

    public bool TryGet(decimal lat, decimal lon, out (string gym, string park) data, int? precision = null) =>
      precision.HasValue ?
        myGymLowerPrecisionInfo.TryGetValue(RaidHelpers.LowerPrecision(lat, lon, LowerDecimalPrecision), out data) :
        myGymInfo.TryGetValue((lat, lon), out data);
  }
}