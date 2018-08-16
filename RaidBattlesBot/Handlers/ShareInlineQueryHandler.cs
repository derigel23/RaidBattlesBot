using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelegateDecompiler.EntityFramework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

namespace RaidBattlesBot.Handlers
{
  [InlineQueryHandler(QueryPattern = "^share")]
  public class ShareInlineQueryHandler : IInlineQueryHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;
    private readonly IUrlHelper myUrlHelper;
    private readonly UserInfo myUserInfo;
    private readonly IClock myClock;
    private readonly RaidService myRaidService;

    public ShareInlineQueryHandler(RaidBattlesContext context, ITelegramBotClient bot, IUrlHelper urlHelper, UserInfo userInfo, IClock clock, RaidService raidService)
    {
      myContext = context;
      myBot = bot;
      myUrlHelper = urlHelper;
      myUserInfo = userInfo;
      myClock = clock;
      myRaidService = raidService;
    }

    async Task<bool?> IHandler<InlineQuery, object, bool?>.Handle(InlineQuery data, object context, CancellationToken cancellationToken)
    {
      var queryParts = data.Query.Split(':');
      if (queryParts[0] != "share")
        return null;

      ICollection<InlineQueryResultBase> inlineQueryResults;
      if (!int.TryParse(queryParts.ElementAtOrDefault(1) ?? "", out var pollid))
      {
        inlineQueryResults = await GetActivePolls(data.From, cancellationToken);
      }
      else
      {
        var poll = (await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { PollId = pollid }, myUrlHelper, cancellationToken))?.Poll;

        inlineQueryResults = new List<InlineQueryResultBase>();
        if (poll != null)
        {
          inlineQueryResults.Add(await poll.ClonePoll(myUrlHelper, myUserInfo, cancellationToken));

          if (poll.Raid() is Raid raid)
          {
            inlineQueryResults.Add(
              new InlineQueryResultVenue($"location:{raid.Id}", (float)raid.Lat, (float)raid.Lon, raid.Title, "Запостить локу")
              {
                ThumbUrl = myUrlHelper.AssetsContent("static_assets/png/ic_map.png").ToString(),
                InputMessageContent = new InputVenueMessageContent(raid.Title, RaidEx.Delimeter.JoinNonEmpty(raid.Gym ?? raid.PossibleGym, raid.Description),
                  (float) raid.Lat, (float) raid.Lon)
              });
          }
          
          if (poll.Portal is Portal portal)
          {
            inlineQueryResults.Add(
              new InlineQueryResultVenue($"location:{portal.Guid}", (float)portal.Latitude, (float)portal.Longitude, portal.Name, "Запостить локу")
              {
                ThumbUrl = myUrlHelper.AssetsContent("static_assets/png/ic_map.png").ToString(),
                InputMessageContent = new InputVenueMessageContent(portal.Name, portal.Address, (float) portal.Latitude, (float) portal.Longitude)
              });
          }

        }
      }

      await myBot.AnswerInlineQueryAsync(data.Id, inlineQueryResults.ToArray(), cacheTime: 0, cancellationToken: cancellationToken);
      return true;
    }

    public async Task<InlineQueryResultArticle[]> GetActivePolls(User user, CancellationToken cancellationToken = default)
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

      return await
        Task.WhenAll(polls.Select(poll => poll.ClonePoll(myUrlHelper, myUserInfo, cancellationToken)));
    }
  }
}