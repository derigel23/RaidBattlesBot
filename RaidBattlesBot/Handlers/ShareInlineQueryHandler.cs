using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelegateDecompiler.EntityFramework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;

namespace RaidBattlesBot.Handlers
{
  [InlineQueryHandler(QueryPrefix = "share")]
  public class ShareInlineQueryHandler : IInlineQueryHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;
    private readonly IUrlHelper myUrlHelper;
    private readonly UserInfo myUserInfo;
    private readonly IClock myClock;

    public ShareInlineQueryHandler(RaidBattlesContext context, ITelegramBotClient bot, IUrlHelper urlHelper, UserInfo userInfo, IClock clock)
    {
      myContext = context;
      myBot = bot;
      myUrlHelper = urlHelper;
      myUserInfo = userInfo;
      myClock = clock;
    }

    async Task<bool?> IHandler<InlineQuery, object, bool?>.Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var queryParts = data.Query.Split(':');
      if (queryParts[0] != "share")
        return null;

      ICollection<InlineQueryResult> inlineQueryResults;
      if (!int.TryParse(queryParts.ElementAtOrDefault(1) ?? "", out var pollid))
      {
        inlineQueryResults = await GetActivePolls(data.From, cancellationToken);
      }
      else
      {
        var poll = await myContext.Polls
          .Where(_ => _.Id == pollid)
          .IncludeRelatedData()
          .FirstOrDefaultAsync(cancellationToken);

        inlineQueryResults = new List<InlineQueryResult>();
        if (poll != null)
        {
          inlineQueryResults.Add(await poll.ClonePoll(myUrlHelper, myUserInfo, cancellationToken));

          if (poll.Raid() is Raid raid)
          {
            inlineQueryResults.Add(
              new InlineQueryResultVenue
              {
                Id = $"location:{raid.Id}",
                Title = raid.Title,
                Address = "Запостить локу",
                Latitude = (float) raid.Lat,
                Longitude = (float) raid.Lon,
                ThumbUrl = myUrlHelper.AssetsContent("static_assets/png/ic_map.png").ToString(),
                InputMessageContent = new InputVenueMessageContentNew
                {
                  Name = raid.Title,
                  Address = RaidEx.Delimeter.JoinNonEmpty(raid.Gym ?? raid.PossibleGym, raid.Description),
                  Latitude = (float) raid.Lat,
                  Longitude = (float) raid.Lon,
                },
              });
          }
        }
      }

      return await myBot.AnswerInlineQueryAsync(data.Id, inlineQueryResults.ToArray(), cacheTime: 0, cancellationToken: cancellationToken);
    }

    public async Task<InlineQueryResultArticle[]> GetActivePolls(User user, CancellationToken cancellationToken = default)
    {
      var userId = user.Id;
      var now = myClock.GetCurrentInstant().ToDateTimeOffset();

      // active raids or polls
      var polls = await myContext.Polls
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

    [JsonObject(Title = "InputVenueMessageContent", MemberSerialization = MemberSerialization.OptIn, NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    private class InputVenueMessageContentNew : InputVenueMessageContent
    {
      /// <summary>Name of the venue</summary>
      [JsonProperty("title", Required = Required.Always)]
      public string Title { get => Name; set => Name = value; }
    }
  }
}