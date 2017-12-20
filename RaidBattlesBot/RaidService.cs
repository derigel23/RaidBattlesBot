using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
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

    public async Task<bool> AddRaid(string text, PollMessage message, CancellationToken cancellationToken = default)
    {
      Poll poll;
      var raid = new Raid
      {
        Title = text,
        Polls = new List<Poll>
        {
          (poll = new Poll
          {
            Owner = message.UserId,
            Messages = new List<PollMessage> { message }
          })
        }
      };

      myContext.Raids.Attach(raid);
      await myContext.SaveChangesAsync(cancellationToken);

      return await AddPollMessage(message, cancellationToken);
    }

    public async Task<bool> AddPollMessage(PollMessage message, CancellationToken cancellationToken = default )
    {
      myContext.Attach(message);

      var messageText = message.Poll.GetMessageText().ToString();
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
      var messageText = poll.GetMessageText().ToString();
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