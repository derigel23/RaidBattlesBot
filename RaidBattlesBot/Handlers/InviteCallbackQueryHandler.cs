using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class InviteCallbackQueryHandler : ICallbackQueryHandler
  {
    public const string ID = "invite";

    private readonly RaidBattlesContext myContext;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly ITelegramBotClient myBot;

    public InviteCallbackQueryHandler(RaidBattlesContext context, RaidService raidService, IUrlHelper urlHelper, ITelegramBotClient bot)
    {
      myContext = context;
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myBot = bot;
    }

    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data?.Split(':');
      if (callback?[0] != ID)
        return (null, false, null);

      if (!(data.Message?.Chat is {} chat))
        return ("Not supported", false, null);
      
      PollMessage pollMessage;
      if (callback.ElementAtOrDefault(1) is var pollIdSegment && PollEx.TryGetPollId(pollIdSegment, out var pollId, out var format))
      {
        pollMessage = await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { BotId = myBot.BotId, PollId = pollId }, myUrlHelper, format, cancellationToken);
      }
      else
      {
        return ("Poll is publishing. Try later.", true, null);
      }

      if (pollMessage?.Poll is var poll && poll == null)
        return ("Poll is not found", true, null);

      var inviteMessage = await poll.GetInviteMessage(myContext, cancellationToken);
      inviteMessage ??= new InputTextMessageContent("Nobody to invite");

      await myBot.SendTextMessageAsync(chat, inviteMessage, disableNotification: true, cancellationToken: cancellationToken);
      
      return (null, false, null);
    }
  }
}