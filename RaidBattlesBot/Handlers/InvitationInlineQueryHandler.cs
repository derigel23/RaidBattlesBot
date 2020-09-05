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
  public class InvitationInlineQueryHandler : IInlineQueryHandler
  {
    public const string PREFIX = "invite:";

    private readonly IUrlHelper myUrlHelper;
    private readonly RaidService myRaidService;
    private readonly ITelegramBotClientEx myBot;
    private readonly RaidBattlesContext myDB;

    public InvitationInlineQueryHandler(IUrlHelper urlHelper, RaidService raidService, ITelegramBotClientEx bot, RaidBattlesContext db)
    {
      myUrlHelper = urlHelper;
      myRaidService = raidService;
      myBot = bot;
      myDB = db;
    }
    
    private const int InvitationBatchSize = 5;
    
    public async Task<bool?> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var queryParts = new StringSegment(data.Query).Split(new[] {':'});

      if (!PollEx.TryGetPollId(queryParts.ElementAtOrDefault(1), out var pollId, out var format))
        return null;
      
      var poll = (await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { PollId = pollId }, myUrlHelper, format, cancellationToken))?.Poll;
      if (poll == null)
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
      var i = 0;
      var invitePartitionedVotes = from vote in inviteVotes
        group vote by i++ / InvitationBatchSize into parts
        select parts;
      
      var invitations = invitePartitionedVotes.Select(votes =>
      {
        var resultNicknames = votes
          .Select(vote => nicknames.TryGetValue(vote.UserId, out var nickname) ? nickname : vote.Username)
          .Where(_ => !string.IsNullOrEmpty(_))
          .ToList();
        if (resultNicknames.Count == 0) return null;
        return new InlineQueryResultArticle(PREFIX + poll.GetInlineId(suffixNumber: votes.Key),
          $"Invite {votes.Key * InvitationBatchSize + 1} - {(votes.Key + 1) * InvitationBatchSize}",
          new StringBuilder()
            .Code((builder, mode) =>
              builder.Sanitize(
                string.Join(",", resultNicknames), mode))
            .ToTextMessageContent())
        {
          ThumbUrl = myUrlHelper.AssetsContent("static_assets/png/btn_new_party.png").ToString()
        };
      });

      var result = invitations.Where(invitation => invitation != null).ToArray();
      if (result.Length == 0)
      {
        result = new[]
        {
          new InlineQueryResultArticle("NobodyToInvite", "Nobody to invite",
            new StringBuilder().Sanitize($"Nobody to invite").ToTextMessageContent())
          {
            ThumbUrl = myUrlHelper.AssetsContent(@"static_assets/png/btn_close_normal.png").ToString()
          }
        };
      }

      await myBot.AnswerInlineQueryWithValidationAsync(data.Id, result, cacheTime: 0, isPersonal: true, cancellationToken: cancellationToken);
      return true;

    }
  }
}