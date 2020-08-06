using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelegateDecompiler.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using NodaTime;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot.Handlers
{
  [InlineQueryHandler(QueryPattern = "^" + ID)]
  public class ShareInlineQueryHandler : IInlineQueryHandler
  {
    public const string ID = "share";
    
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClientEx myBot;
    private readonly IUrlHelper myUrlHelper;
    private readonly IClock myClock;
    private readonly RaidService myRaidService;

    public ShareInlineQueryHandler(RaidBattlesContext context, ITelegramBotClientEx bot, IUrlHelper urlHelper, IClock clock, RaidService raidService)
    {
      myContext = context;
      myBot = bot;
      myUrlHelper = urlHelper;
      myClock = clock;
      myRaidService = raidService;
    }

    async Task<bool?> IHandler<InlineQuery, object, bool?>.Handle(InlineQuery data, object context, CancellationToken cancellationToken)
    {
      var queryParts = new StringSegment(data.Query).Split(new[] {':'});
      if (queryParts.First() != ID)
        return null;

      var inlineQueryResults = Enumerable.Empty<InlineQueryResultBase>();
      if (!PollEx.TryGetPollId(queryParts.ElementAtOrDefault(1), out var pollId, out var format))
      {
        inlineQueryResults = await GetActivePolls(data.From, cancellationToken);
      }
      else
      {
        var poll = (await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { PollId = pollId }, myUrlHelper, format, cancellationToken))?.Poll;

        var queryResults = new List<InlineQueryResultBase>();
        if (poll != null)
        {
          queryResults.Add(poll.ClonePoll(myUrlHelper));

          if (poll.Raid() is { } raid)
          {
            queryResults.Add(
              new InlineQueryResultVenue($"location:{raid.Id}", (float)raid.Lat, (float)raid.Lon, raid.Title, "Share a location")
              {
                ThumbUrl = myUrlHelper.AssetsContent("static_assets/png/ic_map.png").ToString(),
                InputMessageContent = new InputVenueMessageContent(raid.Title, RaidEx.Delimeter.JoinNonEmpty(raid.Gym ?? raid.PossibleGym, raid.Description),
                  (float) raid.Lat, (float) raid.Lon)
              });
          }
          
          if (poll.Portal is Portal portal)
          {
            queryResults.Add(
              new InlineQueryResultVenue($"location:{portal.Guid}", (float)portal.Latitude, (float)portal.Longitude, portal.Name, "Share a location")
              {
                ThumbUrl = myUrlHelper.AssetsContent("static_assets/png/ic_map.png").ToString(),
                InputMessageContent = new InputVenueMessageContent(portal.Name, portal.Address, (float) portal.Latitude, (float) portal.Longitude)
              });
          }
          inlineQueryResults = queryResults;
        }
      }

      await myBot.AnswerInlineQueryWithValidationAsync(data.Id, inlineQueryResults.ToArray(), cacheTime: 0, cancellationToken: cancellationToken);
      return true;
    }

    public async Task<IReadOnlyCollection<InlineQueryResultArticle>> GetActivePolls(User user, CancellationToken cancellationToken = default)
    {
      return Array.Empty<InlineQueryResultArticle>();
      var userId = user.Id;
      var now = myClock.GetCurrentInstant().ToDateTimeOffset();

      // active raids or polls
      var polls = await myContext
        .Set<Poll>()
        .IncludeRelatedData()
        .Where(_ => _.EndTime > now) // live poll
        .Where(_ => _.Raid.EggRaidId == null) // no eggs if boss is already known
        .Where(_ => _.Owner == userId || _.Votes.Any(vote => vote.UserId == userId))
        .OrderBy(_ => _.EndTime)
        //.Take(10)
        .DecompileAsync()
        .ToArrayAsync(cancellationToken);

      return Array.ConvertAll(polls, (poll => poll.ClonePoll(myUrlHelper)));
    }
  }
}