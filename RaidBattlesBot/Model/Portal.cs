using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace RaidBattlesBot.Model
{
  [DebuggerDisplay("{Name} ({Guid})")]
  public class Portal : ITrackable
  {
    [JsonProperty(Required = Required.Always)]
    public string Guid { get; set; }
    [JsonProperty(Required = Required.Always)]
    public string Name { get; set; }
    public string Address { get; set; }
    public string Image { get; set; }
    [JsonProperty("lng", Required = Required.Always)]
    public decimal Longitude { get; set; }
    [JsonProperty("lat", Required = Required.Always)]
    public decimal Latitude { get; set; }
    
    public decimal[] GetCoordinates()
    {
      return new[] { Latitude, Longitude };
    }

    public DateTimeOffset? Modified { get; set; }
  }
}