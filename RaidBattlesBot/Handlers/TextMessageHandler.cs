#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType = MessageType.Text)]
  public class TextMessageHandler : TextMessageHandler<PollMessage, bool?, MessageEntityTypeAttribute>, IMessageHandler
  {
    private readonly IEnumerable<Lazy<Func<Message, IReplyBotCommandHandler>, BotCommandAttribute>> myReplyCommandHandlers;
    private readonly RaidBattlesContext myDb;

    public TextMessageHandler(ITelegramBotClient bot, IEnumerable<Lazy<Func<Message, IReplyBotCommandHandler>, BotCommandAttribute>> replyCommandHandlers,
        IEnumerable<Lazy<Func<Message, IMessageEntityHandler<PollMessage, bool?>>, MessageEntityTypeAttribute>> messageEntityHandlers,
        RaidBattlesContext db, IClock clock)
      : base(bot, messageEntityHandlers)
    {
      myReplyCommandHandlers = replyCommandHandlers;
      myDb = db;
    }

    public override async Task<bool?> Handle(Message message, (UpdateType updateType, PollMessage context) _, CancellationToken cancellationToken = default)
    {
      if (string.IsNullOrEmpty(message.Text))
        return false;

      var (_, pollMessage) = _;
      if (message.ForwardFromChat is { } forwarderFromChat)
      {
        var forwardedPollMessage = await myDb
          .Set<PollMessage>()
          .Where(pm=> pm.ChatId == forwarderFromChat.Id && pm.MessageId == message.ForwardFromMessageId)
          .IncludeRelatedData()
          .FirstOrDefaultAsync(cancellationToken);

        if (forwardedPollMessage != null)
        {
          pollMessage.Poll = forwardedPollMessage.Poll;
          return true;
        }
      }

      // check for reply commands
      // handle only replies without other commands
      if (message.ReplyToMessage is { } parentMessage &&
          (message.Entities?.All(entity => entity.Type != MessageEntityType.BotCommand) ?? true ))
      {
        if (await Handle(message, parentMessage, pollMessage, myReplyCommandHandlers, cancellationToken) is {} replyProcessed)
        {
          return replyProcessed;
        }
      }

      return await base.Handle(message, _, cancellationToken);
    }
  }
}