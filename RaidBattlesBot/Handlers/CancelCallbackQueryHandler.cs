using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class CancelCallbackQueryHandler : ICallbackQueryHandler
  {
    public const string ID = "cancel";
    
    private readonly RaidBattlesContext myContext;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly ChatInfo myChatInfo;

    public CancelCallbackQueryHandler(RaidBattlesContext context, RaidService raidService, IUrlHelper urlHelper, ChatInfo chatInfo)
    {
      myContext = context;
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myChatInfo = chatInfo;
    }

    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != "cancel")
        return (null, false, null);
      
      if (!PollEx.TryGetPollId(callback.ElementAtOrDefault(1), out var pollId, out var format))
        return ("Голование подготавливается. Повторите позже", true, null);

      var poll = (await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { PollId = pollId }, myUrlHelper, format, cancellationToken))?.Poll;

      if (poll == null)
        return ("Голосование не найдено", true, null);

      var user = data.From;

      if (!await myChatInfo.CandEditPoll(poll.Owner, user.Id, cancellationToken))
        return ("Вы не можете отменить голосование", true, null);

      poll.Cancelled = true;
      var changed = myContext.SaveChanges() > 0;
      if (changed)
      {
        await myRaidService.UpdatePoll(poll, myUrlHelper, cancellationToken);
      }

      return (changed ? "Голосование отменено" : "Голование уже отменено", false, null);
    }
  }
}