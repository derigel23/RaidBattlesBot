using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  public class ChosenInlineResultHandler : IChosenInlineResultHandler
  {
    private readonly ITelegramBotClient myBot;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly PoGoToolsClient myPoGoToolsClient;

    public ChosenInlineResultHandler(ITelegramBotClient bot, RaidService raidService, IUrlHelper urlHelper, PoGoToolsClient poGoToolsClient)
    {
      myBot = bot;
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myPoGoToolsClient = poGoToolsClient;
    }

    public async Task<bool?> Handle(ChosenInlineResult data, object context = default, CancellationToken cancellationToken = default)
    {
      var resultParts = data.ResultId.Split(':');
      switch (resultParts[0])
      {
        case PollEx.InlineIdPrefix:
          if (!PollEx.TryGetPollId(resultParts.ElementAtOrDefault(1), out var pollId, out var format))
            return null;
          var message = new PollMessage(data) { BotId = myBot.BotId, PollId = pollId };
          if (Enum.TryParse<PollMode>(resultParts.ElementAtOrDefault(3), out var pollMode))
          {
            message.PollMode = pollMode;
          }
          var pollMessage = await myRaidService.GetOrCreatePollAndMessage(message, myUrlHelper, format, cancellationToken);
          if (pollMessage != null)
          {
            if (pollMessage.Poll is { } poll &&  (poll.Portal?.Guid ?? poll.PortalId) is { } guid)
            {
              await myPoGoToolsClient.UpdateWayspot(guid, poll.ExRaidGym ? Wayspot.ExRaidGym : Wayspot.Gym, cancellationToken);
            }

            return true;
          }
          
          return false;
      }

      return null;
    }
  }
}