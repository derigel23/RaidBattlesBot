using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Team23.TelegramSkeleton;

namespace RaidBattlesBot.Handlers
{
  public class StatusProvider : IStatusProvider
  {
    private readonly IUrlHelper myUrlHelper;

    public StatusProvider(IUrlHelper urlHelper)
    {
      myUrlHelper = urlHelper;
    }
    
    public Task<IDictionary<string, object>> Handle(IDictionary<string, object> status, ControllerContext context, CancellationToken cancellationToken = default)
    {
      status["assetsRoot"] = myUrlHelper.AssetsContent("~");
      return Task.FromResult(status);
    }
  }
}