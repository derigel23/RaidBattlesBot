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
using Telegram.Bot;
using Telegram.Bot.Types;

using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot.Pages
{
  public class MapModel : PageModel
  {
    public User BotUser { get; private set; }
    
    private readonly RaidBattlesContext myDb;
    private readonly IClock myClock;
    private readonly ITelegramBotClient myBot;

    public MapModel(RaidBattlesContext db, IClock clock, ITelegramBotClient bot)
    {
      myDb = db;
      myClock = clock;
      myBot = bot;
    }
    
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
      BotUser = await myBot.GetMeAsync(cancellationToken);
      return Page();
    }

    public async Task<IActionResult> OnHeadAsync(CancellationToken cancellationToken)
    {
      return new OkResult();
    }

    [Produces(typeof(JsonpMediaTypeFormatter))]
    public async Task<IActionResult> OnGetDataAsync(string bbox, CancellationToken cancellationToken)
    {
      var parts = bbox.Split(',', 4);
      var latLow = decimal.Parse(parts[0], NumberStyles.Currency, CultureInfo.InvariantCulture);
      var lonLow = decimal.Parse(parts[1], NumberStyles.Currency, CultureInfo.InvariantCulture);
      var latHigh = decimal.Parse(parts[2], NumberStyles.Currency, CultureInfo.InvariantCulture);
      var lonHigh = decimal.Parse(parts[3], NumberStyles.Currency, CultureInfo.InvariantCulture);

      var now = myClock.GetCurrentInstant().ToDateTimeOffset();;
      var polls = await myDb
        .Set<Poll>()
        .IncludeRelatedData()
        .Where(_ => _.Raid.RaidBossEndTime > now)
        .Where(_ => _.Raid.EggRaidId == null) // no eggs if boss is already known
        .Where(_ => _.Raid.Lat >= latLow && _.Raid.Lat <= latHigh)
        .Where(_ => _.Raid.Lon >= lonLow && _.Raid.Lon <= lonHigh)
        .DecompileAsync()
        .ToArrayAsync(cancellationToken);
      
      var response = new
      {
        type = "FeatureCollection",
        features = polls.Select(p => new
        {
          type = "Feature",
          id = p.Id,
          geometry = new
          {
            type = "Point",
            coordinates = new [] { p.Raid.Lat.GetValueOrDefault(), p.Raid.Lon.GetValueOrDefault() }
          },
          properties = new
          {
            id = p.Id,
            raidLevel = $"R{p.Raid.RaidBossLevel}",
            name = p.Raid.Name,
            img = p.GetThumbUrl(Url),
            title = p.GetTitle(),
            description = $"{p.Raid.Description}",
          },
          options = new
          {
            preset = p.Raid.IsEgg ? "islands#icon" : "islands#dotIcon",
            iconColor = p.Raid.GetEggColor() is Color c ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : null,
          }
        })
      };

      return new OkObjectResult(response);
    }
  }
}