using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Memory;
using RaidBattlesBot.Handlers;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Controllers
{
  public class TelegramController : TelegramController<PollMessage>
  {
    private readonly RaidService myRaidService;
    private readonly IMemoryCache myCache;

    public TelegramController(TelemetryClient telemetryClient,
      ITelegramBotClient telegramBotClient, RaidService raidService, IMemoryCache cache, 
      IEnumerable<Meta<Func<Message, IGenericMessageHandler<PollMessage>>,MessageTypeAttribute>> messageHandlers,
      IEnumerable<Meta<Func<Update, ICallbackQueryHandler>, CallbackQueryHandlerAttribute>> callbackQueryHandlers,
      IEnumerable<Meta<Func<Update, IInlineQueryHandler>, InlineQueryHandlerAttribute>> inlineQueryHandlers,
      IEnumerable<Func<Update, IChosenInlineResultHandler>> chosenInlineResultHandlers)
      : base(telemetryClient, telegramBotClient, messageHandlers, callbackQueryHandlers, inlineQueryHandlers, chosenInlineResultHandlers)
    {
      myRaidService = raidService;
      myCache = cache;
    }

    protected override async Task<bool?> ProcessMessage(Func<Message, PollMessage, IDictionary<string, string>, CancellationToken, Task<bool?>> processor, Message message, CancellationToken cancellationToken)
    {
      var pollMessage = new PollMessage(message);

      var result = await processor(message, pollMessage, pollMessage.GetTrackingProperties(), cancellationToken);
      if (result is bool success)
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
      else if ((message.ForwardFrom == null) && (message.ForwardFromChat == null) && (message.Type == MessageType.Text) && ((message.Entities?.Length).GetValueOrDefault() == 0))
      {
        myCache.Set(message.Chat.Id, message, TimeSpan.FromSeconds(15));
      }

      return result;
    }
  }
}