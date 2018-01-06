using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using DelegateDecompiler;

namespace RaidBattlesBot.Model
{
    public class Raid : ITrackable
    {
      private static readonly TimeSpan BossLifetime = TimeSpan.FromMinutes(45);

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

      [Computed, NotMapped]
      public DateTimeOffset? RaidBossEndTime
      {
        get
        {
          if ((RaidBossLevel != null) && (Pokemon == null)) // egg
          {
            return EndTime?.Add(BossLifetime); // plus boss lifetime
          }

          return EndTime;
        }
        set
        {
          if ((RaidBossLevel != null) && (Pokemon == null)) // egg
          {
            EndTime = value?.Add(-BossLifetime); // minus boss lifetime
            return;
          }

          EndTime = value;
      }
      }

      public int? EggRaidId { get; set; }
      public Raid EggRaid { get; set; }
      public Raid PostEggRaid { get; set; }
    }
}
