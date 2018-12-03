using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;

namespace Team23.TelegramSkeleton
{
  public abstract class StatusController : Controller
  {
    private readonly ITelegramBotClient myBot;

    protected StatusController(ITelegramBotClient bot)
    {
      myBot = bot;
    }

    [HttpGet("/status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
      return Json(await GetStatusData(cancellationToken));
    }

    protected virtual async Task<dynamic> GetStatusData(CancellationToken cancellationToken)
    {
      dynamic result = new ExpandoObject();
      result.webhookInfo = await myBot.GetWebhookInfoAsync(cancellationToken);
      result.botInfo = await myBot.GetMeAsync(cancellationToken);
      return result;
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