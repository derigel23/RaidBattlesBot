using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot
{
  public class RaidService
  {
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;
    private readonly TelemetryClient myTelemetryClient;
    private readonly IHttpContextAccessor myHttpContextAccessor;

    public RaidService(RaidBattlesContext context, ITelegramBotClient bot, TelemetryClient telemetryClient, IHttpContextAccessor httpContextAccessor)
    {
      myContext = context;
      myBot = bot;
      myTelemetryClient = telemetryClient;
      myHttpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> AddRaid(Raid raid, PollMessage message, CancellationToken cancellationToken = default)
    {
      (raid.Polls ?? (raid.Polls = new List<Poll>(1))).Add(new Poll
      {
        Owner = message.UserId,
        Messages = new List<PollMessage>(1) { message }
      });

      myContext.Attach(raid);
      await myContext.SaveChangesAsync(cancellationToken);

      return await AddPollMessage(message, cancellationToken);
    }

    public async Task<bool> AddPoll(string text, PollMessage message, CancellationToken cancellationToken = default)
    {
      message.Poll = new Poll
      {
        Title = text,
        Owner = message.UserId
      };

      return await AddPollMessage(message, cancellationToken);
    }


    public async Task<bool> AddPollMessage(PollMessage message, CancellationToken cancellationToken = default )
    {
      myContext.Attach(message);
      if (await myContext.SaveChangesAsync(cancellationToken) == 0)
        return false; //nothing changed

      var messageText = message.Poll.GetMessageText();
      if (message.InlineMesssageId == null)
      {
        var postedMessage = await myBot.SendTextMessageAsync(message.Chat, messageText, ParseMode.Markdown,
          replyMarkup: message.GetReplyMarkup(), disableNotification: true, cancellationToken: cancellationToken);
        message.Chat = postedMessage.Chat;
        message.MesssageId = postedMessage.MessageId;
      }
      else
      {
        await myBot.EditInlineMessageTextAsync(message.InlineMesssageId, messageText, ParseMode.Markdown,
          replyMarkup: message.GetReplyMarkup(), cancellationToken: cancellationToken);
      }

      return await myContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task UpdatePoll(Poll poll, CancellationToken cancellationToken = default)
    {
      var messageText = poll.GetMessageText();
      foreach (var message in poll.Messages)
      {
        try
        {
          if (message.InlineMesssageId != null)
          {
            await myBot.EditInlineMessageTextAsync(message.InlineMesssageId, messageText, ParseMode.Markdown,
              replyMarkup: message.GetReplyMarkup(), cancellationToken: cancellationToken);
          }
          else
          {
            await myBot.EditMessageTextAsync(message.Chat, message.MesssageId.GetValueOrDefault(), messageText, ParseMode.Markdown,
              replyMarkup: message.GetReplyMarkup(), cancellationToken: cancellationToken);
          }
        }
        catch (Exception ex)
        {
          myTelemetryClient.TrackException(ex, myHttpContextAccessor.HttpContext.Properties());
        }
      }
    }
  }
}