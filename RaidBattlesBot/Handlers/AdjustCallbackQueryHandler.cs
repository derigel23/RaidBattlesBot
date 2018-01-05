using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = "adjust")]
  public class AdjustCallbackQueryHandler : ICallbackQueryHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;

    public AdjustCallbackQueryHandler(RaidBattlesContext context, RaidService raidService, IUrlHelper urlHelper)
    {
      myContext = context;
      myRaidService = raidService;
      myUrlHelper = urlHelper;
    }

    public async Task<(string, bool)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != "adjust")
        return (null, false);
      
      if (!int.TryParse(callback.ElementAtOrDefault(1) ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out var pollId))
        return ("Голование подготавливается. Повторите позже", true);

      if (!int.TryParse(callback.ElementAtOrDefault(2) ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out var offset))
        return ("", false);

      var poll = await myContext
        .Polls
        .Where(_ => _.Id == pollId)
        .Include(_ => _.Votes)
        .Include(_ => _.Messages)
        .Include(_ => _.Raid)
        .FirstOrDefaultAsync(cancellationToken);

      if (poll == null)
        return ("Голосование не найдено", true);

      var user = data.From;

      if (poll.Owner != user.Id)
        return ("Вы не можете редактировать голосование", true);

      poll.Time = poll.Time?.AddMinutes(offset);
      if (poll.Time > poll.Raid?.RaidBossEndTime())
        return ($"В {poll.Time:t} рейд уже закончится", true);

      var changed = await myContext.SaveChangesAsync(cancellationToken) > 0;
      if (changed)
      {
        await myRaidService.UpdatePoll(poll, myUrlHelper, cancellationToken);
      }

      return ($"Голосование установлено на {poll.Time:t}", false);
    }
  }
}