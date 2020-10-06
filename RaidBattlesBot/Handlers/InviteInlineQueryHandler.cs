using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot.Handlers
{
  [InlineQueryHandler(QueryPattern = "^" + PREFIX)]
  public class InviteInlineQueryHandler : IInlineQueryHandler
  {
    public const string PREFIX = "invite:";

    private readonly IUrlHelper myUrlHelper;
    private readonly RaidService myRaidService;
    private readonly ITelegramBotClientEx myBot;
    private readonly RaidBattlesContext myDB;

    public InviteInlineQueryHandler(IUrlHelper urlHelper, RaidService raidService, ITelegramBotClientEx bot,
      RaidBattlesContext db)
    {
      myUrlHelper = urlHelper;
      myRaidService = raidService;
      myBot = bot;
      myDB = db;
    }

    public async Task<bool?> Handle(InlineQuery data, object context = default,
      CancellationToken cancellationToken = default)
    {
      var queryParts = new StringSegment(data.Query).Split(new[] {' '}).FirstOrDefault().Split(new[] {':'});

      if (!PollEx.TryGetPollId(queryParts.ElementAtOrDefault(1), out var pollId, out var format))
        return null;

      var poll = (await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) {PollId = pollId}, myUrlHelper,
        format, cancellationToken))?.Poll;
      if (poll == null)
        return null;

      var result = await GetResult(poll, cancellationToken);

      result ??= new InlineQueryResultArticle("NobodyToInvite", "Nobody to invite",
        new StringBuilder().Sanitize($"Nobody to invite").ToTextMessageContent())
        {
          ThumbUrl = myUrlHelper.AssetsContent(@"static_assets/png/btn_close_normal.png").ToString()
        };

      await myBot.AnswerInlineQueryWithValidationAsync(data.Id, new[] { result }, cacheTime: 0, isPersonal: true, cancellationToken: cancellationToken);
      return true;
    }

    public async Task<InlineQueryResultBase> GetResult(Poll poll, CancellationToken cancellationToken)
    {
      var inviteMessage = await poll.GetInviteMessage(myDB, cancellationToken);
      
      if (inviteMessage != null)
      {
        return new InlineQueryResultArticle(PREFIX + poll.GetInlineId(), "Invite", inviteMessage)
        {
          ThumbUrl = myUrlHelper.AssetsContent("static_assets/png/btn_new_party.png").ToString()
        };
      }

      return null;
    }
  }
}