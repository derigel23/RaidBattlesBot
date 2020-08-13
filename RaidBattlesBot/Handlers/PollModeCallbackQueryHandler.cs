using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class PollModeCallbackQueryHandler : ICallbackQueryHandler
  {
    public const string ID = "mode";
    
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly RaidBattlesContext myDb;

    public PollModeCallbackQueryHandler(RaidService raidService, IUrlHelper urlHelper, RaidBattlesContext db)
    {
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myDb = db;
    }
    
    public async Task<(string text, bool showAlert, string url)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != ID)
        return (null, false, null);
      
      if (!PollEx.TryGetPollId(callback.ElementAtOrDefault(1), out var pollId, out var format))
        return ("Poll is publishing. Try later.", true, null);

      if (!Enum.TryParse<PollMode>(callback.ElementAtOrDefault(2) ?? "", out var pollMode))
        return ("", false, null);

      var pollMessage = await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { PollId = pollId }, myUrlHelper, format, cancellationToken);

      if (pollMessage.Poll == null)
        return ("Poll is not found.", true, null);

      pollMessage.PollMode = pollMode;
      var changed = await myDb.SaveChangesAsync(cancellationToken) > 0;
      if (changed)
      {
        await myRaidService.UpdatePollMessage(pollMessage, myUrlHelper, cancellationToken);
        return ("Mode have been switched", false, null);
      }

      return (null, false, null);
    }
  }
}