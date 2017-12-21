using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  public class ChosenInlineResultHandler : IChosenInlineResultHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly RaidService myRaidService;

    public ChosenInlineResultHandler(RaidBattlesContext context, RaidService raidService)
    {
      myContext = context;
      myRaidService = raidService;
    }

    public async Task<bool> Handle(ChosenInlineResult data, object context = default, CancellationToken cancellationToken = default)
    {
      var resultParts = data.ResultId.Split(':');
      if (resultParts[0] == "poll" && int.TryParse(resultParts.ElementAtOrDefault(1) ?? "", out var pollId))
      {
        myContext.Messages.Add(new PollMessage
        {
          PollId = pollId,
          InlineMesssageId = data.InlineMessageId
        });
        return await myContext.SaveChangesAsync(cancellationToken) > 0;
      }
      else if (resultParts[0] == "create")
      {
        return await myRaidService.AddPoll(data.Query, new PollMessage(data), cancellationToken);
      }

      return false;
    }
  }
}