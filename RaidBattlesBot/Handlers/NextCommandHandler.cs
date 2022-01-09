using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [BotCommand("next" ,"Show upcoming raids", BotCommandScopeType.AllPrivateChats)]
  public class NextCommandHandler : IBotCommandHandler
  {
    private readonly RaidBattlesContext myDB;
    private readonly IClock myClock;
    private readonly ITelegramBotClient myBot;

    private readonly TimeSpan myJitterOffset = TimeSpan.FromSeconds(23);
    
    public NextCommandHandler(RaidBattlesContext db,  IClock clock, ITelegramBotClient bot)
    {
      myDB = db;
      myClock = clock;
      myBot = bot;
    }
    
    public async Task<bool?> Handle(MessageEntityEx entity, PollMessage context = default, CancellationToken cancellationToken = default)
    {
      if (!this.ShouldProcess(entity, context)) return null;

      var from = myClock.GetCurrentInstant().ToDateTimeOffset() - myJitterOffset;
      var polls = await myDB.Set<Poll>()
        .Where(poll => poll.Time >= from)
        .Where(poll => poll.Votes.Any(vote => vote.UserId == entity.Message.From.Id))
        .IncludeRelatedData()
        .ToListAsync(cancellationToken);

      var builder = new TextBuilder();
      if (polls.Count == 0)
      {
        builder.Append($"No upcoming raids.");
      }
      else
      {
        foreach (var poll in polls.OrderBy(poll => poll.Time))
        {
          builder.Bold(b =>
             b.Code(bb => bb.AppendFormat("{0:t} ", poll.Time)));
          poll.GetTitle(builder).NewLine();
        }
      }

      var content = builder.ToTextMessageContent(disableWebPreview: true);
      await myBot.SendTextMessageAsync(entity.Message.Chat, content, true, cancellationToken: cancellationToken);

      return false;
    }
  }
}