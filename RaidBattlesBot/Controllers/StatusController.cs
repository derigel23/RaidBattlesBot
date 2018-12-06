using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;

namespace RaidBattlesBot.Controllers
{
  public class StatusController : Team23.TelegramSkeleton.StatusController
  {
    private readonly IUrlHelper myUrlHelper;

    public StatusController(ITelegramBotClient bot, IUrlHelper urlHelper) : base(bot)
    {
      myUrlHelper = urlHelper;
    }

    protected override async Task<dynamic> GetStatusData(CancellationToken cancellationToken)
    {
      var statusData = await base.GetStatusData(cancellationToken);
      statusData.is64BitProcess = Environment.Is64BitProcess;
      statusData.assetsRoot = myUrlHelper.AssetsContent("~");
      return statusData;
    }
  }
}