using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.TextMessage)]
  public class TextMessageHandler : IMessageHandler
  {
    private readonly RaidService myRaidService;
    private readonly IEnumerable<Meta<Func<Message, IMessageEntityHandler>, MessageEntityTypeAttribute>> myMessageEntityHandlers;

    public TextMessageHandler(RaidService raidService, IEnumerable<Meta<Func<Message, IMessageEntityHandler>, MessageEntityTypeAttribute>> messageEntityHandlers)
    {
      myRaidService = raidService;
      myMessageEntityHandlers = messageEntityHandlers;
    }

    public async Task<bool> Handle(Message message, object context = default , CancellationToken cancellationToken = default)
    {
      if (string.IsNullOrEmpty(message.Text))
        return true;

      var handlers = myMessageEntityHandlers.Bind(message).ToList();
      foreach (var entity in message.Entities)
      {
        return await HandlerExtentions<bool>.Handle(handlers, entity, new object(), cancellationToken);
      }

      return false;
    }
  }
}