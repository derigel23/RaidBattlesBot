using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  public class MessageUpdateHandler : MessageUpdateHandler<IMessageHandler, PollMessage, bool?, MessageTypeAttribute>
  {
    private readonly RaidService myRaidService;
    private readonly IMemoryCache myCache;
    private readonly IUrlHelper myUrlHelper;

    public MessageUpdateHandler(RaidService raidService, IMemoryCache cache, IUrlHelper urlHelper, IEnumerable<Meta<Func<Message, IMessageHandler>, MessageTypeAttribute>> messageHandlers)
      : base(messageHandlers)
    {
      myRaidService = raidService;
      myCache = cache;
      myUrlHelper = urlHelper;
    }
    
    protected override async Task<bool?> ProcessMessage(Func<Message, PollMessage, IDictionary<string, string>, CancellationToken, Task<bool?>> processor, Message message, CancellationToken cancellationToken = default)
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
              await myRaidService.AddPollMessage(pollMessage, myUrlHelper, cancellationToken, withLog: true);
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