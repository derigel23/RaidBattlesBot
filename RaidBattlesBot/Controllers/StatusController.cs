using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;

namespace RaidBattlesBot.Controllers
{
  public class StatusController : Controller
  {
    private readonly ITelegramBotClient myBot;
    private readonly IUrlHelper myUrlHelper;

    public StatusController(ITelegramBotClient bot, IUrlHelper urlHelper)
    {
      myBot = bot;
      myUrlHelper = urlHelper;
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
  }
}