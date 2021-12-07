using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.Text)]
  public class TimeZoneNotifyHandler : IMessageHandler
  {
    private readonly TelemetryClient myTelemetryClient;
    private readonly ITelegramBotClient myBot;
    private readonly RaidBattlesContext myDB;
    private readonly TimeZoneNotifyService myTimeZoneNotifyService;

    public TimeZoneNotifyHandler(TelemetryClient telemetryClient, ITelegramBotClient bot, RaidBattlesContext db, TimeZoneNotifyService timeZoneNotifyService)
    {
      myTelemetryClient = telemetryClient;
      myBot = bot;
      myDB = db;
      myTimeZoneNotifyService = timeZoneNotifyService;
    }
    
    /// processing delay for incoming messages to allow <see cref="ChosenInlineResultHandler"/> handle message first
    private static readonly TimeSpan ourDelayProcessing = TimeSpan.FromSeconds(1);

    public async Task<bool?> Handle(Message message, (UpdateType updateType, PollMessage context) context = default, CancellationToken cancellationToken = default)
    {
      if (message is { ReplyMarkup: { InlineKeyboard: {} inlineKeyboard} })
      {
        foreach (var buttons in inlineKeyboard)
        foreach (var button in buttons)
        {
          if (button.CallbackData is { } buttonCallbackData && buttonCallbackData.Split(":", 3) is {} callbackDataParts)
          {
            if (callbackDataParts.Length > 1 && callbackDataParts[0] == VoteCallbackQueryHandler.ID)
            {
              if (PollEx.TryGetPollId(callbackDataParts[1], out var pollId, out _))
              {
                try
                {
                  var res = await myTimeZoneNotifyService.ProcessPoll(myBot, message.Chat.Id, message.MessageId, async ct =>
                  {
                    await Task.Delay(ourDelayProcessing, ct);

                    return await myDB
                      .Set<Model.Poll>()
                      .Where(p => p.Id == pollId)
                      .IncludeRelatedData()
                      .Include(poll => poll.Notifications)
                      .FirstOrDefaultAsync(cancellationToken);
                  }, () => new TextBuilder(), cancellationToken);

                  await myDB.SaveChangesAsync(cancellationToken);
                  return res;
                }
                catch (Exception ex)
                {
                  if (ex is ApiRequestException { ErrorCode: 403 })
                  {
                  }
                  else
                    myTelemetryClient.TrackExceptionEx(ex, properties: new Dictionary<string, string>
                    {
                      { nameof(ITelegramBotClient.BotId), myBot?.BotId.ToString() },
                      { "UserId", message.From?.Id.ToString() }
                    });
                }
              }
            }
          }
        }
      }

      return default;
    }
  }
}