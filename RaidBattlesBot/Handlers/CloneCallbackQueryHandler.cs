using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = "clone")]
  public class CloneCallbackQueryHandler : ICallbackQueryHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly User myBot;

    public CloneCallbackQueryHandler(RaidBattlesContext context, RaidService raidService, IUrlHelper urlHelper, User bot)
    {
      myContext = context;
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myBot = bot;
    }

    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != "clone")
        return (null, false, null);
      
      if (!int.TryParse(callback.ElementAtOrDefault(1) ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out var pollId))
        return ("Голование подготавливается. Повторите позже", true, null);

      var poll = await myContext
        .Set<Poll>()
        .Where(_ => _.Id == pollId)
        .IncludeRelatedData()
        .FirstOrDefaultAsync(cancellationToken);

      if (poll == null)
        return ("Голосование не найдено", true, null);

      var pollMessage = new PollMessage
      {
        ChatId =  data.From.Id,
        ChatType = ChatType.Private,
        UserId = data.From.Id,
        InlineMesssageId = data.InlineMessageId,
        Poll = poll
      };
      await myRaidService.AddPollMessage(pollMessage, myUrlHelper, cancellationToken);
      
      return (null, false, $"https://t.me/{myBot.Username}?start={pollMessage.GetPollId()}");
    }
  }
}