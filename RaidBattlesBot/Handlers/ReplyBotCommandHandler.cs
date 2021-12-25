#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers;

public interface IReplyBotCommandHandler :  IBotCommandHandler { }

public abstract  class ReplyBotCommandHandler : IReplyBotCommandHandler
{
  private readonly Message myMessage;

  protected ReplyBotCommandHandler(Message message)
  {
    myMessage = message;
  }
  
  public async Task<bool?> Handle(MessageEntityEx entity, PollMessage? context = default, CancellationToken cancellationToken = default)
  {
    if (this.ShouldProcess(entity, context))
    {
      var text = entity.AfterValue.Trim();
      if (entity.Message != myMessage) // reply mode
      {
        text = myMessage.Text;
      }

      return await Handle(myMessage, text, context, cancellationToken);
    }

    return null;
  }

  protected abstract Task<bool?> Handle(Message message, StringSegment text, PollMessage? context = default, CancellationToken cancellationToken = default);
}