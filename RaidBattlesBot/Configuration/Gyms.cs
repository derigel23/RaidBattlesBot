using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using CsvHelper;
using RaidBattlesBot.Model;

// ReSharper disable PossibleNullReferenceException

namespace RaidBattlesBot.Configuration
{
  public class Gyms
  {
    public const int LowerDecimalPrecision = 4;
      
    private readonly IDictionary<(decimal lat, decimal lon), string> myGymInfo = new ConcurrentDictionary<(decimal lat, decimal lon), string>();
    private readonly IDictionary<(decimal lat, decimal lon), string> myGymLowerPrecisionInfo = new ConcurrentDictionary<(decimal lat, decimal lon), string>();
    
    public Gyms(Stream stream)
    {
      using (var streamReader = new StreamReader(stream, Encoding.UTF8))
      {
        var configuration = new CsvHelper.Configuration.Configuration
        {
          BadDataFound = context =>
            context.FieldBuilder.Clear().Append(
              context.RawRecord.Split(',', 2).ElementAtOrDefault(1)?.TrimEnd() is string quotedName ?
                quotedName.StartsWith("\"") ?
                  quotedName.Substring(1) is string unquotedNameStart ?
                    unquotedNameStart.EndsWith("\"") ? unquotedNameStart.Substring(0, unquotedNameStart.Length - 1) : unquotedNameStart : quotedName :
                  quotedName :
                null)
        };
        
        using (var csvReader = new CsvReader(streamReader, configuration))
        {
          foreach (var record in csvReader.GetRecords(new { latlon = default(string), name = default(string) }))
          {
            var lonPos = record.latlon.LastIndexOf('.') - 2;
            if (lonPos < 0) continue;
            if (decimal.TryParse(record.latlon.Substring(0, lonPos), NumberStyles.Currency, CultureInfo.InvariantCulture, out var lat) &&
                decimal.TryParse(record.latlon.Substring(lonPos), NumberStyles.Currency, CultureInfo.InvariantCulture, out var lon))
            {
              var name = record.name;
              if (string.IsNullOrEmpty(name)) continue;
              if (name[0] == '"')
              {
                name = name.Substring(1, name.Length - 1 - (name[name.Length - 1] == '"' ? 1 : 0)); // unquote
              }
              myGymInfo.Add((lat, lon), name);
              myGymLowerPrecisionInfo[RaidHelpers.LowerPrecision(lat, lon, LowerDecimalPrecision)] = name;
            }
          }
        }
      }
    }

    public bool TryGet(decimal lat, decimal lon, out string gym, int? precision = null) =>
      precision is int specifiedPrecision ?
        myGymLowerPrecisionInfo.TryGetValue(RaidHelpers.LowerPrecision(lat, lon, specifiedPrecision), out gym) :
        myGymInfo.TryGetValue((lat, lon), out gym);
  }
}