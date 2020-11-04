using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class CloneCallbackQueryHandler : ICallbackQueryHandler
  {
    public const string ID = "clone";

    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly ITelegramBotClient myBot;

    public CloneCallbackQueryHandler(RaidService raidService, IUrlHelper urlHelper, ITelegramBotClient bot)
    {
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myBot = bot;
    }

    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != ID)
        return (null, false, null);
      
      PollMessage originalPollMessage;
      if (callback.ElementAtOrDefault(1) is var pollIdSegment && PollEx.TryGetPollId(pollIdSegment, out var pollId, out var format))
      {
        originalPollMessage = await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { BotId = myBot.BotId, PollId = pollId }, myUrlHelper, format, cancellationToken);
      }
      else
      {
        return ("Poll is publishing. Try later.", true, null);
      }

      if (originalPollMessage?.Poll is var poll && poll == null)
        return ("Poll is not found", true, null);
      
      var clonedPollMessage = new PollMessage
      {
        BotId = myBot.BotId,
        ChatId =  data.From.Id,
        ChatType = ChatType.Private,
        UserId = data.From.Id,
        InlineMessageId = data.InlineMessageId,
        Poll = poll
      };
      
      await myRaidService.AddPollMessage(clonedPollMessage, myUrlHelper, cancellationToken);

      var botUser = await myBot.GetMeAsync(cancellationToken);
      return (null, false, $"https://t.me/{botUser.Username}?start={clonedPollMessage.GetExtendedPollId()}");
    }
  }
}