using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;

namespace Team23.TelegramSkeleton
{
  public class StatusController : Controller
  {
    private readonly ITelegramBotClient myBot;
    private readonly IEnumerable<IStatusProvider> myStatusProviders;

    public StatusController(ITelegramBotClient bot, IEnumerable<IStatusProvider> statusProviders)
    {
      myBot = bot;
      myStatusProviders = statusProviders;
    }

    [HttpGet("/status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
      return Json(await GetStatusData(cancellationToken).ConfigureAwait(false));
    }

    private async Task<IDictionary<string, object>> GetStatusData(CancellationToken cancellationToken)
    {
      dynamic status = new ExpandoObject();
      status.botInfo = await myBot.GetMeAsync(cancellationToken);
      status.webhookInfo = await myBot.GetWebhookInfoAsync(cancellationToken);
      status.is64BitProcess = System.Environment.Is64BitProcess;
      foreach (var statusProvider in myStatusProviders)
      {
        await statusProvider.Handle(status, ControllerContext, cancellationToken);
      }

      return status;
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

      await myBot.GetUpdatesAsync(-1, 1, cancellationToken: cancellationToken);

      return RedirectToAction("Status");
    }
  }

  public interface IStatusProvider : IHandler<IDictionary<string, object>, ControllerContext, IDictionary<string, object>>
  {
  }
}