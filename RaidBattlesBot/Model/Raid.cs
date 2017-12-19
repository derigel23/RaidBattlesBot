using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RaidBattlesBot.Model
{
    public class Raid : ITrackable
    {
      public int Id { get; set; }
      public decimal? Lat { get; set; }
      public decimal? Lon { get; set; }
      public string Title { get; set; }
      public string Description { get; set; }
      public int? RaidBossLevel { get; set; }
      public int? Pokemon { get; set; }
      public string Name { get; set; }
      public string Gym { get; set; }
      public string PossibleGym { get; set; }
      public DateTimeOffset? StartTime { get; set; }
      public DateTimeOffset? EndTime { get; set; }
      public string Move1 { get; set; }
      public string Move2 { get; set; }
      public string NearByPlaceId { get; set; }
      public string NearByAddress { get; set; }
      public DateTimeOffset? Modified { get; set; }

      public List<Poll> Polls { get; set; }
    }
}
