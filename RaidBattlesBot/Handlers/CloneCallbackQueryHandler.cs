﻿using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class CloneCallbackQueryHandler : ICallbackQueryHandler
  {
    public const string ID = "clone";

    private readonly RaidBattlesContext myContext;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly ITelegramBotClient myBot;

    public CloneCallbackQueryHandler(RaidBattlesContext context, RaidService raidService, IUrlHelper urlHelper, ITelegramBotClient bot)
    {
      myContext = context;
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myBot = bot;
    }

    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != ID)
        return (null, false, null);
      
      if (!int.TryParse(callback.ElementAtOrDefault(1) ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out var pollId))
        return ("Poll is publishing. Try later.", true, null);

      var poll = await myContext
        .Set<Poll>()
        .Where(_ => _.Id == pollId)
        .IncludeRelatedData()
        .FirstOrDefaultAsync(cancellationToken);

      if (poll == null)
        return ("Poll is not found", true, null);

      var pollMessage = new PollMessage
      {
        ChatId =  data.From.Id,
        ChatType = ChatType.Private,
        UserId = data.From.Id,
        InlineMesssageId = data.InlineMessageId,
        Poll = poll
      };
      await myRaidService.AddPollMessage(pollMessage, myUrlHelper, cancellationToken);

      var botUser = await myBot.GetMeAsync(cancellationToken);
      return (null, false, $"https://t.me/{botUser.Username}?start={pollMessage.GetExtendedPollId()}");
    }
  }
}