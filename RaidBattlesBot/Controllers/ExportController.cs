using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;

namespace RaidBattlesBot.Controllers
{
  public class ExportController : Controller
  {
    private readonly RaidBattlesContext myDB;

    public ExportController(RaidBattlesContext db)
    {
      myDB = db;
    }

    [Route("/export/gyms")]
    public async Task<IActionResult> gyms(CancellationToken cancellationToken = default)
    {
      var portals = await myDB.Set<Portal>().ToListAsync(cancellationToken);
      
      return Json(new
      {
        gyms = portals
          .Select(portal => new { guid = portal.Guid.ToString(), lat = portal.Latitude, lng = portal.Longitude, name = portal.Name, image = portal.Image })
          .Aggregate(new ExpandoObject(), (obj, _ ) =>
          {
            obj.TryAdd(_.guid, _);
            return obj;
          })
      });
    }
  }
}