using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = "cancel")]
  public class CancelCallbackQueryHandler : ICallbackQueryHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly RaidService myRaidService;

    public CancelCallbackQueryHandler(RaidBattlesContext context, RaidService raidService)
    {
      myContext = context;
      myRaidService = raidService;
    }

    public async Task<string> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != "cancel")
        return null;
      
      if (!int.TryParse(callback.ElementAt(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var pollId))
        return null;

      var poll = await myContext
        .Polls
        .Where(_ => _.Id == pollId)
        .Include(_ => _.Votes)
        .Include(_ => _.Messages)
        .Include(_ => _.Raid)
        .FirstOrDefaultAsync(cancellationToken);

      if (poll == null)
        return null;

      var user = data.From;

      if (poll.Owner != user.Id)
        return "Вы не можете отменить голосование";

      poll.Cancelled = true;
      var changed = await myContext.SaveChangesAsync(cancellationToken) > 0;
      if (changed)
      {
        await myRaidService.UpdatePoll(poll, cancellationToken);
      }

      return changed ? $"Голосование отменено" : "Голование уже отменено";
    }
  }
}