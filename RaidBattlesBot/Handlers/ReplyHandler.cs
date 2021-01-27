using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageTypeAttribute.AllMessageTypes)]
  public class ReplyHandler : IMessageHandler
  {
    private readonly TelemetryClient myTelemetryClient;
    private readonly ITelegramBotClient myBot;
    private readonly RaidBattlesContext myDB;

    public ReplyHandler(TelemetryClient telemetryClient, ITelegramBotClient bot, RaidBattlesContext db)
    {
      myTelemetryClient = telemetryClient;
      myBot = bot;
      myDB = db;
    }
    
    public async Task<bool?> Handle(Message message, (UpdateType updateType, PollMessage context) context = default, CancellationToken cancellationToken = default)
    {
      if (message.ReplyToMessage is { ReplyMarkup: { InlineKeyboard: {} parentInlineKeyboard} } parentMessage)
      {
        foreach (var buttons in parentInlineKeyboard)
        foreach (var button in buttons)
        {
          if (button.CallbackData is { } buttonCallbackData)
          {
            foreach (var part in buttonCallbackData.Split(':'))
            {
              if (PollEx.TryGetPollId(part, out var pollId, out var format))
              {
                var poll = await myDB
                  .Set<Model.Poll>()
                  .Where(p => p.Id == pollId)
                  .IncludeRelatedData()
                  .FirstOrDefaultAsync(cancellationToken);

                if (poll == null) return default;
                
                context.context = new PollMessage(message)
                {
                  BotId = myBot.BotId,
                  MessageId = message.MessageId,
                  PollId = pollId,
                  Poll = poll
                };
                foreach (var pollVote in poll.Votes)
                {
                  var botId = pollVote.BotId;
                  var userId = pollVote.UserId;
                  if (userId == message.From.Id) continue; // do not notify itself
                  if (!(pollVote.Team?.HasAnyFlags(VoteEnum.Going) ?? false)) continue; // do not notify bailed people
                  try
                  {
                    await myBot.ForwardMessageAsync(userId, message.Chat, message.MessageId, cancellationToken: cancellationToken);
                  }
                  catch (Exception ex)
                  {
                    if (ex is ForbiddenException)
                    {
                    }
                    else
                      myTelemetryClient.TrackExceptionEx(ex, properties: new Dictionary<string, string>
                      {
                        { nameof(ITelegramBotClient.BotId), botId?.ToString() },
                        { "UserId", userId.ToString() }
                      });
                  }
                }
                return true;
              }
            }
          }
        }
      }

      return default;
    }
  }
}