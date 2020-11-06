using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class NotifyCallbackQueryHandler : ICallbackQueryHandler
  {
    public const string ID = "notify";

    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly ITelegramBotClient myBot;
    private readonly TelemetryClient myTelemetryClient;
    private readonly NotifyInlineQueryHandler myNotifyInlineQueryHandler;

    public NotifyCallbackQueryHandler(RaidService raidService, IUrlHelper urlHelper, ITelegramBotClient bot, TelemetryClient telemetryClient, NotifyInlineQueryHandler notifyInlineQueryHandler)
    {
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myBot = bot;
      myTelemetryClient = telemetryClient;
      myNotifyInlineQueryHandler = notifyInlineQueryHandler;
    }

    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != ID)
        return (null, false, null);

      if (!(data.Message.Chat is {} chat))
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

      var inviteMessages = await myNotifyInlineQueryHandler.GenerateMessages(poll, "", cancellationToken);
      if (inviteMessages != null)
      {
        if (inviteMessages.Length == 0)
        {
          inviteMessages = new[] { new InputTextMessageContent("Nobody to notify") };
        }

        foreach (var message in inviteMessages)
        {
          try
          {
            await myBot.SendTextMessageAsync(chat, message.MessageText, message.ParseMode,
              message.DisableWebPagePreview, disableNotification: true, cancellationToken: cancellationToken);
          }
          catch (Exception ex)
          {
            myTelemetryClient.TrackExceptionEx(ex, properties: pollMessage.GetTrackingProperties(new Dictionary<string, string>
            {
              { nameof(ITelegramBotClient.BotId), myBot.BotId.ToString() }
            }));
          }
        }
      }

      return (null, false, null);
    }
  }
}