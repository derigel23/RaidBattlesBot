using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelegateDecompiler.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using RaidBattlesBot.Model;

namespace RaidBattlesBot.Pages
{
  public class PortalModel : PageModel
  {
    private readonly RaidBattlesContext myDb;
    private readonly ZonedClock myClock;
    private readonly IngressClient myIngressClient;

    public Portal Portal { get; set; }
    
    public PortalModel(RaidBattlesContext db, ZonedClock clock, IngressClient ingressClient)
    {
      myDb = db;
      myClock = clock;
      myIngressClient = ingressClient;
    }

    public async Task<IActionResult> OnGetAsync(string guid, CancellationToken cancellationToken)
    {
      Portal = guid is string g ? await myIngressClient.Get(g, cancellationToken: cancellationToken) : null;

      return Page();
    }

    public async Task<IActionResult> OnHeadAsync(CancellationToken cancellationToken)
    {
      return new OkResult();
    }

    [Produces(typeof(JsonpMediaTypeFormatter))]
    public async Task<IActionResult> OnGetDataAsync(string bbox, CancellationToken cancellationToken)
    {
      return new OkObjectResult(await GetData(bbox, cancellationToken));
    }

    private async Task<object> GetData(string bbox, CancellationToken cancellationToken)
    {
      var parts = bbox.Split(',', 4);
      var latLow = decimal.Parse(parts[0], NumberStyles.Currency, CultureInfo.InvariantCulture);
      var lonLow = decimal.Parse(parts[1], NumberStyles.Currency, CultureInfo.InvariantCulture);
      var latHigh = decimal.Parse(parts[2], NumberStyles.Currency, CultureInfo.InvariantCulture);
      var lonHigh = decimal.Parse(parts[3], NumberStyles.Currency, CultureInfo.InvariantCulture);

      ZonedDateTime StartOfDay(ZonedDateTime zonedDateTime) =>
        zonedDateTime.Date.AtStartOfDayInZone(zonedDateTime.Zone);

      var portals = await myDb.Set<Portal>()
        .Where(_ => _.Latitude >= latLow && _.Latitude <= latHigh)
        .Where(_ => _.Longitude >= lonLow && _.Longitude <= lonHigh)
        .DecompileAsync()
        .ToArrayAsync(cancellationToken);

      return PrepareData(portals);
    }

    public object PrepareData(params Portal[] portals)
    {
      return new
      {
        type = "FeatureCollection",
        features = portals.Select(p => new
        {
          type = "Feature",
          id = p.Guid,
          geometry = new
          {
            type = "Point",
            coordinates = p.GetCoordinates()
          },
          properties = new
          {
            id = p.Guid,
            portal = new { p.Guid, p.Name, p.Address, Image = p.GetImage(Url, fallbackToDefault: false).ForceHttps() },
          },
          options = new
          {
            //preset = "islands#icon",
            iconColor = Color.Red is Color c ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : null,
          }
        })
      };
    }
  }
}