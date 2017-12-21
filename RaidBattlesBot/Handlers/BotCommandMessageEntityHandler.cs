using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageEntityType(EntityType = MessageEntityType.BotCommand)]
  public class BotCommandMessageEntityHandler : IMessageEntityHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly RaidService myRaidService;
    private readonly Message myMessage;
    private readonly ITelegramBotClient myBot;

    public BotCommandMessageEntityHandler(RaidBattlesContext context, RaidService raidService, Message message, ITelegramBotClient bot)
    {
      myContext = context;
      myRaidService = raidService;
      myMessage = message;
      myBot = bot;
    }

    public async Task<bool> Handle(MessageEntity entity, object context = default, CancellationToken cancellationToken = default)
    {
      var command = myMessage.Text.Substring(entity.Offset, entity.Length);
      switch (command)
      {
        case var _ when command.StartsWith("/new"):
          var title = myMessage.Text.Substring(entity.Offset + entity.Length).Trim();
          if (string.IsNullOrEmpty(title)) return false;
          return await myRaidService.AddPoll(title, new PollMessage(myMessage), cancellationToken);

        case var _ when command.StartsWith("/poll"):
          if (!int.TryParse(myMessage.Text.Substring(entity.Offset + entity.Length).Trim(), out var pollId))
            return false;
          var poll = await myContext.Polls
            .Where(_ => _.Id == pollId)
            .Include(_ => _.Raid)
            .Include(_ => _.Votes)
            .FirstOrDefaultAsync(cancellationToken);
          if (poll == null)
            return false;
          return await myRaidService.AddPollMessage(new PollMessage(myMessage) { Poll = poll }, cancellationToken);
      }

      return false;
    }
  }
}