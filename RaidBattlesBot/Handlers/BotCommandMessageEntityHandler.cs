using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageEntityType(EntityType = MessageEntityType.BotCommand)]
  public class BotCommandMessageEntityHandler : IMessageEntityHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly Message myMessage;

    public BotCommandMessageEntityHandler(RaidBattlesContext context, Message message)
    {
      myContext = context;
      myMessage = message;
    }

    public async Task<bool?> Handle(MessageEntity entity, PollMessage pollMessage, CancellationToken cancellationToken = default)
    {
      var command = myMessage.Text.Substring(entity.Offset, entity.Length);
      switch (command)
      {
        case var _ when command.StartsWith("/new"):
          var title = myMessage.Text.Substring(entity.Offset + entity.Length).Trim();
          if (string.IsNullOrEmpty(title)) return false;
          pollMessage.Poll = new Poll(myMessage)
          {
            Title = title
          };
          return true;

        case var _ when command.StartsWith("/poll"):
          if (!int.TryParse(myMessage.Text.Substring(entity.Offset + entity.Length).Trim(), out var pollId))
            return false;
          var existingPoll = await myContext.Polls
            .Where(_ => _.Id == pollId)
            .IncludeRelatedData()
            .FirstOrDefaultAsync(cancellationToken);
          if (existingPoll == null)
            return false;
          pollMessage.Poll = existingPoll;
          return true;
      }

      return null;
    }
  }
}