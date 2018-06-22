using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using RaidBattlesBot.Handlers;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Controllers
{
  public class TelegramController : Controller
  {
    private readonly TelemetryClient myTelemetryClient;
    private readonly ITelegramBotClient myTelegramBotClient;
    private readonly RaidService myRaidService;
    private readonly IMemoryCache myCache;
    private readonly IEnumerable<Meta<Func<Message, IMessageHandler>, MessageTypeAttribute>> myMessageHandlers;
    private readonly IEnumerable<Meta<Func<Update, ICallbackQueryHandler>, CallbackQueryHandlerAttribute>> myCallbackQueryHandlers;
    private readonly IEnumerable<Meta<Func<Update, IInlineQueryHandler>, InlineQueryHandlerAttribute>> myInlineQueryHandlers;
    private readonly IEnumerable<Func<Update, IChosenInlineResultHandler>> myChosenInlineResultHandlers;

    public TelegramController(TelemetryClient telemetryClient,
      ITelegramBotClient telegramBotClient, RaidService raidService, IMemoryCache cache, 
      IEnumerable<Meta<Func<Message, IMessageHandler>, MessageTypeAttribute>> messageHandlers,
      IEnumerable<Meta<Func<Update, ICallbackQueryHandler>, CallbackQueryHandlerAttribute>> callbackQueryHandlers,
      IEnumerable<Meta<Func<Update, IInlineQueryHandler>, InlineQueryHandlerAttribute>> inlineQueryHandlers,
      IEnumerable<Func<Update, IChosenInlineResultHandler>> chosenInlineResultHandlers)
    {
      myTelemetryClient = telemetryClient;
      myTelegramBotClient = telegramBotClient;
      myRaidService = raidService;
      myCache = cache;
      myMessageHandlers = messageHandlers;
      myCallbackQueryHandlers = callbackQueryHandlers;
      myInlineQueryHandlers = inlineQueryHandlers;
      myChosenInlineResultHandlers = chosenInlineResultHandlers;
    }

    [HttpPost("/update")]
    public async Task<IActionResult> Update([CanBeNull, FromBody] Update update, CancellationToken cancellationToken = default)
    {
      IActionResult Return(bool? result) =>
        result is bool success && success ? Ok() : Ok(); // TODO: not handled

      PollMessage pollMessage = null;

      try
      {
        if (update == null)
        {
          foreach (var errorEntry in ModelState)
          {
            myTelemetryClient.Context.Properties[$"ModelState.{errorEntry.Key}"] = errorEntry.Value.AttemptedValue;
            var errors = errorEntry.Value.Errors;
            for (var i = 0; i < errors.Count; i++)
            {
              myTelemetryClient.Context.Properties[$"ModelState.{errorEntry.Key}.{i}"] = errors[i].ErrorMessage;
              if (errors[i].Exception is Exception exception)
              {
                myTelemetryClient.TrackException(exception, new Dictionary<string, string> { { errorEntry.Key, errorEntry.Value.AttemptedValue } });
              };
            }
          }
          throw new ArgumentNullException(nameof(update));
        }

        Message message = null;
        switch (update.Type)
        {
          case UpdateType.MessageUpdate:
            message = update.Message;
            break;

          case UpdateType.ChannelPost:
            message = update.ChannelPost;
            break;

          case UpdateType.CallbackQueryUpdate:
            var callbackQuery = update.CallbackQuery;
            HttpContext.Items["uid"] = callbackQuery.From?.Username;
            HttpContext.Items["data"] = callbackQuery.Data;
            (var text, var showAlert, string url) = await HandlerExtentions<(string, bool, string)>.Handle(myCallbackQueryHandlers.Bind(update), callbackQuery, new object(), cancellationToken);
            return Return(await myTelegramBotClient.AnswerCallbackQueryAsync(callbackQuery.Id, text, showAlert, url, cancellationToken: cancellationToken));

          case UpdateType.InlineQueryUpdate:
            var inlineQuery = update.InlineQuery;
            HttpContext.Items["uid"] = inlineQuery.From?.Username;
            HttpContext.Items["query"] = inlineQuery.Query;
            return Return(await HandlerExtentions<bool?>.Handle(myInlineQueryHandlers.Bind(update), inlineQuery, new object(), cancellationToken));

          case UpdateType.ChosenInlineResultUpdate:
            var chosenInlineResult = update.ChosenInlineResult;
            HttpContext.Items["uid"] = chosenInlineResult.From?.Username;
            HttpContext.Items["query"] = chosenInlineResult.Query;
            HttpContext.Items["result"] = chosenInlineResult.ResultId;
            return Return(await HandlerExtentions<bool?>.Handle(myChosenInlineResultHandlers.Bind(update), chosenInlineResult, new object(), cancellationToken));
        }

        if (message == null)
          return Ok();

        myTelemetryClient.Context.User.AccountId = (message.From?.Id ?? message.ForwardFrom?.Id)?.ToString();
        myTelemetryClient.Context.Properties["uid"] = myTelemetryClient.Context.User.AuthenticatedUserId =
          message.From?.Username ?? message.ForwardFrom?.Username;
        myTelemetryClient.Context.Properties["messageType"] = message.Type.ToString();
        myTelemetryClient.Context.Properties["chat"] = message.Chat.Username;

        pollMessage = new PollMessage(message);
        if ((await HandlerExtentions<bool?>.Handle(myMessageHandlers.Bind(message), message, pollMessage, cancellationToken)) is bool success)
        {
          if (success)
          {
            if (string.IsNullOrEmpty(pollMessage.Poll.Title) &&
                myCache.TryGetValue<Message>(message.Chat.Id, out var prevMessage) &&
                (prevMessage.From?.Id == message.From?.Id))
            {
              pollMessage.Poll.Title = prevMessage.Text;
              myCache.Remove(message.Chat.Id);
            }

            switch (pollMessage.Poll?.Raid)
            {
              // regular pokemons in private chat
              case Raid raid when raid.RaidBossLevel == null && message.Chat?.Type == ChatType.Private:
                goto case null;

              // raid pokemons everywhere
              case Raid raid when raid.RaidBossLevel != null:
                goto case null;

              // polls without raids
              case null:
                await myRaidService.AddPollMessage(pollMessage, Url, cancellationToken);
                break;
            }
          }
        }
        else if ((message.ForwardFrom == null) && (message.ForwardFromChat == null) && (message.Type == MessageType.TextMessage) && (message.Entities.Count == 0))
        {
          myCache.Set(message.Chat.Id, message, TimeSpan.FromSeconds(15));
        }

        return Ok() /* TODO: not handled */;

      }
      catch (Exception ex)
      {
        myTelemetryClient.TrackExceptionEx(ex, pollMessage.GetTrackingProperties());
        return Ok();
      }
      finally
      {
        var eventName = update?.Type.ToString();
        if (eventName != null)
        {
          myTelemetryClient.TrackEvent(eventName, pollMessage.GetTrackingProperties());
        }
      }
    }
  }
}