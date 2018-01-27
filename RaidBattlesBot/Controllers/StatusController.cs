using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Configuration;
using Telegram.Bot;

namespace RaidBattlesBot.Controllers
{
  public class StatusController : Controller
  {
    private readonly ITelegramBotClient myBot;
    private readonly IUrlHelper myUrlHelper;
    private readonly Gyms myGyms;

    public StatusController(ITelegramBotClient bot, IUrlHelper urlHelper, Gyms gyms)
    {
      myBot = bot;
      myUrlHelper = urlHelper;
      myGyms = gyms;
    }

    [HttpGet("/status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
      var webhookInfo = await myBot.GetWebhookInfoAsync(cancellationToken);
      var botInfo = await myBot.GetMeAsync(cancellationToken);
      return Json(new { botInfo, webhookInfo, assetsRoot = myUrlHelper.AssetsContent("~") });
    }

    [HttpGet("/refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
      var webHookUrl = Url.Action("Update", "Telegram", null, protocol: "https");
      
      await myBot.SetWebhookAsync(webHookUrl, cancellationToken: cancellationToken);

      return RedirectToAction("Status");
    }

    [HttpGet("/clear")]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
      await myBot.SetWebhookAsync("", cancellationToken: cancellationToken);

      return RedirectToAction("Status");
    }
    
    [HttpGet("/gyms")]
    public async Task<IActionResult> Gyms(CancellationToken cancellationToken)
    {
      using (var stream = new MemoryStream())
      using (var writer = new StreamWriter(stream, Encoding.UTF8))
      using (var csvWriter = new CsvWriter(writer))
      {
        foreach (var pair in myGyms.GymInfo)
        {
          csvWriter.WriteField(pair.gym, true);
          csvWriter.WriteField(pair.location.lat.ToString(CultureInfo.InvariantCulture), false);
          csvWriter.WriteField(pair.location.lon.ToString(CultureInfo.InvariantCulture), false);
          csvWriter.NextRecord();
        }

        return new FileContentResult(stream.ToArray(), "text/csv") { FileDownloadName = "gyms.csv"};
      }
    }
  }
}