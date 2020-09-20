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
  public class NotifyInlineQueryHandler : IInlineQueryHandler
  {
    public const string PREFIX = "notify:";

    private readonly IUrlHelper myUrlHelper;
    private readonly RaidService myRaidService;
    private readonly ITelegramBotClientEx myBot;

    public NotifyInlineQueryHandler(IUrlHelper urlHelper, RaidService raidService, ITelegramBotClientEx bot)
    {
      myUrlHelper = urlHelper;
      myRaidService = raidService;
      myBot = bot;
    }
    
    public async Task<bool?> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var queryParts = new StringSegment(data.Query).Split(new []{' '}).FirstOrDefault().Split(new[] {':'});

      if (!PollEx.TryGetPollId(queryParts.ElementAtOrDefault(1), out var pollId, out var format))
        return null;
      
      var poll = (await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { PollId = pollId }, myUrlHelper, format, cancellationToken))?.Poll;
      if (poll == null)
        return null;

      var result = GetResult(data, poll);

      result ??= new InlineQueryResultArticle("NobodyToNotify", "Nobody to notify",
        new StringBuilder().Sanitize($"Nobody to notify").ToTextMessageContent())
        {
          ThumbUrl = myUrlHelper.AssetsContent("static_assets/png/btn_trade_swap.png").ToString()
        };

      await myBot.AnswerInlineQueryWithValidationAsync(data.Id, new[] { result }, cacheTime: 0, isPersonal: true, cancellationToken: cancellationToken);
      return true;
    }

    public InlineQueryResultArticle GetResult(InlineQuery data, Poll poll)
    {
      if (!poll.AllowedVotes?.HasFlag(VoteEnum.Invitation) ?? true)
        return null;

      var inviteVotes = poll.Votes
        .Where(vote => vote.Team?.HasFlag(VoteEnum.Invitation) ?? false)
        .OrderBy(vote => vote.Modified)
        .ToList();

      var initial = data.Query.Split().Skip(1).Aggregate(new StringBuilder(), (builder, s) => builder.Append(s));
      var descriptionLength = initial.Length;

      if (inviteVotes.Count > 0)
      {
        return new InlineQueryResultArticle(PREFIX + poll.GetInlineId(), "Notify",
          inviteVotes.Aggregate(initial.Append(" "),
            (builder, vote) =>
              (string.IsNullOrEmpty(vote.Username)
                ? builder.Append(vote.User.GetLink())
                : builder.Append('@').Append(vote.Username)).Append(", "),
            builder => builder.Remove(builder.Length - 2, 2).ToTextMessageContent()))
        {
          Description = initial.ToString(0, descriptionLength),
          ThumbUrl = myUrlHelper.AssetsContent("static_assets/png/btn_trade_swap.png").ToString()
        };
      }

      return null;
    }
  }
}