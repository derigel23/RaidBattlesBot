using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;

namespace RaidBattlesBot.Handlers
{
  public class StatusProvider : IStatusProvider
  {
    private readonly IUrlHelper myUrlHelper;
    private readonly RaidBattlesContext myDB;

    public StatusProvider(IUrlHelper urlHelper, RaidBattlesContext db)
    {
      myUrlHelper = urlHelper;
      myDB = db;
    }
    
    public async Task<IDictionary<string, object>> Handle(IDictionary<string, object> status, ControllerContext context, CancellationToken cancellationToken = default)
    {
      status["assetsRoot"] = myUrlHelper.AssetsContent("~");
      status["lastAppliedMigration"] = (await myDB.Database.GetAppliedMigrationsAsync(cancellationToken)).LastOrDefault();
      status["polls"] = await myDB.Set<Poll>().LongCountAsync(cancellationToken);
      return status;
    }
  }
}