using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

namespace RaidBattlesBot.Controllers
{
  public class StatusController : Team23.TelegramSkeleton.StatusController
  {
    public StatusController(ITelegramBotClient bot) : base(bot) { }

    protected override async Task<dynamic> GetStatusData(CancellationToken cancellationToken)
    {
      var statusData = await base.GetStatusData(cancellationToken);
      statusData.assetsRoot = Url.AssetsContent("~");
      return statusData;
    }
  }
}