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
    private readonly IDictionary<long, ITelegramBotClient> myBots;
    private readonly RaidBattlesContext myDB;

    public ReplyHandler(TelemetryClient telemetryClient, ITelegramBotClient bot, IDictionary<long, ITelegramBotClient> bots, RaidBattlesContext db)
    {
      myTelemetryClient = telemetryClient;
      myBot = bot;
      myBots = bots;
      myDB = db;
    }
    
    public async Task<bool?> Handle(Message message, (UpdateType updateType, PollMessage context) context = default, CancellationToken cancellationToken = default)
    {
      if (message.ReplyToMessage is { ReplyMarkup: { InlineKeyboard: {} parentInlineKeyboard} })
      {
        foreach (var buttons in parentInlineKeyboard)
        foreach (var button in buttons)
        {
          if (button.CallbackData is { } buttonCallbackData && buttonCallbackData.Split(":", 3) is {} callbackDataParts)
          {
            if (callbackDataParts.Length > 1 && callbackDataParts[0] == VoteCallbackQueryHandler.ID)
            {
              if (PollEx.TryGetPollId(callbackDataParts[1], out var pollId, out _))
              {
                var poll = await myDB
                  .Set<Model.Poll>()
                  .Where(p => p.Id == pollId)
                  .IncludeRelatedData()
                  .FirstOrDefaultAsync(cancellationToken);

                if (poll == null) return default;

                var fromUserId = message.From?.Id;
                var participants = poll.Votes.Select(_ => _.UserId).ToHashSet();
                if (fromUserId is not {} userId || !participants.Contains(userId)) return false; // do not process replies from strangers

                var replyNotifications = myDB.Set<ReplyNotification>();
                
                context.context = new PollMessage(message)
                {
                  BotId = myBot.BotId,
                  MessageId = message.MessageId,
                  PollId = pollId,
                  Poll = poll
                };
                ITelegramBotClient bot = null;
                foreach (var pollVote in poll.Votes)
                {
                  var voteUserId = pollVote.UserId;
                  if (voteUserId == fromUserId) continue; // do not notify itself
                  if (!(pollVote.Team?.HasAnyFlags(VoteEnum.Going) ?? false)) continue; // do not notify bailed people
                  try
                  {
                    if (pollVote.BotId is not {} botId || !myBots.TryGetValue(botId, out bot))
                    {
                      bot = myBot;
                    }
                    var fm = await bot.ForwardMessageAsync(voteUserId, message.Chat, message.MessageId, cancellationToken: cancellationToken);
                    replyNotifications.Add(new ReplyNotification
                    {
                      BotId = bot.BotId,
                      ChatId = fm.Chat.Id,
                      MessageId = fm.MessageId,
                      FromChatId = message.Chat.Id,
                      FromMessageId = message.MessageId,
                      FromUserId = fromUserId,
                      PollId = pollId,
                      Poll = poll
                    });
                  }
                  catch (Exception ex)
                  {
                    if (ex is ApiRequestException { ErrorCode: 403 })
                    {
                    }
                    else
                      myTelemetryClient.TrackExceptionEx(ex, properties: new Dictionary<string, string>
                      {
                        { nameof(ITelegramBotClient.BotId), bot?.BotId .ToString() },
                        { "UserId", voteUserId.ToString() }
                      });
                  }
                }

                await myDB.SaveChangesAsync(cancellationToken);
                
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