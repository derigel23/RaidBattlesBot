using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.TextMessage)]
  public class TextMessageHandler : IMessageHandler
  {
    private readonly Message myMessage;
    private readonly IEnumerable<Meta<Func<Message, IMessageEntityHandler>, MessageEntityTypeAttribute>> myMessageEntityHandlers;

    public TextMessageHandler(Message message, IEnumerable<Meta<Func<Message, IMessageEntityHandler>, MessageEntityTypeAttribute>> messageEntityHandlers)
    {
      myMessage = message;
      myMessageEntityHandlers = messageEntityHandlers;
    }

    public async Task<bool?> Handle(Message message, Raid raid, CancellationToken cancellationToken = default)
    {
      if (string.IsNullOrEmpty(message.Text))
        return false;
      var handlers = myMessageEntityHandlers.Bind(message).ToList();
      bool? result = default;
      foreach (var entity in message.Entities)
      {
        result = await HandlerExtentions<bool?>.Handle(handlers, entity, raid, cancellationToken);
        if (result.HasValue)
          break;
      }

      return result;
    }
  }
}