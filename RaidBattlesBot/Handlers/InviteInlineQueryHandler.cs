using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

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

      if (!poll.AllowedVotes?.HasFlag(VoteEnum.Invitation) ?? true)
        return null;

      var inviteVotes = poll.Votes
        .Where(vote => vote.Team?.HasFlag(VoteEnum.Invitation) ?? false)
        .OrderBy(vote => vote.Modified)
        .ToList();

      var invitees = inviteVotes.Select(vote => vote.UserId).ToList();
      var nicknames = (await myDB.Set<Player>()
          .Where(player => invitees.Contains(player.UserId))
          .ToListAsync(cancellationToken))
        .ToDictionary(player => player.UserId, player => player.Nickname);

      var resultNicknames = inviteVotes
        .Select(vote => nicknames.TryGetValue(vote.UserId, out var nickname) ? nickname : vote.Username)
        .Where(_ => !string.IsNullOrEmpty(_))
        .ToList();

      InlineQueryResultBase result;
      if (resultNicknames.Count > 0)
      {
        result = new InlineQueryResultArticle(PREFIX + poll.GetInlineId(),"Invite",
          new StringBuilder()
              .Code((builder, mode) =>
                builder.Sanitize(string.Join(",", resultNicknames), mode))
              .ToTextMessageContent())
              {
                ThumbUrl = myUrlHelper.AssetsContent("static_assets/png/btn_new_party.png").ToString()
              };
      }
      else
      {
        result = new InlineQueryResultArticle("NobodyToInvite", "Nobody to invite",
            new StringBuilder().Sanitize($"Nobody to invite").ToTextMessageContent())
            {
              ThumbUrl = myUrlHelper.AssetsContent(@"static_assets/png/btn_close_normal.png").ToString()
            };
      }

      await myBot.AnswerInlineQueryWithValidationAsync(data.Id, new[] { result }, cacheTime: 0, isPersonal: true, cancellationToken: cancellationToken);
      return true;
    }
  }
}