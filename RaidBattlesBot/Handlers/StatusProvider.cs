using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;

namespace RaidBattlesBot.Handlers
{
  public class StatusProvider : IStatusProvider
  {
    private readonly IUrlHelper myUrlHelper;
    private readonly RaidBattlesContext myDB;
    private readonly DateTimeZone myDateTimeZone;
    private readonly IClock myClock;

    public StatusProvider(IUrlHelper urlHelper, RaidBattlesContext db, DateTimeZone dateTimeZone, IClock clock)
    {
      myUrlHelper = urlHelper;
      myDB = db;
      myDateTimeZone = dateTimeZone;
      myClock = clock;
    }
    
    public async Task<IDictionary<string, object>> Handle(IDictionary<string, object> status, ControllerContext context, CancellationToken cancellationToken = default)
    {
      status["defaultTimeZone"] = $"{myDateTimeZone.Id}, {myDateTimeZone.GetZoneInterval(myClock.GetCurrentInstant())}";
      status["assetsRoot"] = myUrlHelper.AssetsContent("~");
      status["lastAppliedMigration"] = (await myDB.Database.GetAppliedMigrationsAsync(cancellationToken)).LastOrDefault();
      status["polls"] = await myDB.Set<Poll>().LongCountAsync(cancellationToken);
      status["IGNs"] = await myDB.Set<Player>().LongCountAsync(cancellationToken);
      return status;
    }
  }
}