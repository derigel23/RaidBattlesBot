﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot.Handlers
{
  [InlineQueryHandler]
  public class GeneralInlineQueryHandler : IInlineQueryHandler
  {
    public const string SwitchToGymParameter = "linkToGym";

    private readonly ITelegramBotClientEx myBot;
    private readonly IUrlHelper myUrlHelper;
    private readonly ShareInlineQueryHandler myShareInlineQueryHandler;
    private readonly RaidService myRaidService;
    private readonly IngressClient myIngressClient;
    private readonly RaidBattlesContext myDb;
    private readonly GeoCoderEx myGeoCoder;
    private readonly IClock myClock;
    private readonly TimeZoneService myTimeZoneService;

    public GeneralInlineQueryHandler(ITelegramBotClientEx bot, IUrlHelper urlHelper, ShareInlineQueryHandler shareInlineQueryHandler, RaidService raidService, IngressClient ingressClient, RaidBattlesContext db, GeoCoderEx geoCoder, IClock clock, TimeZoneService timeZoneService)
    {
      myBot = bot;
      myUrlHelper = urlHelper;
      myShareInlineQueryHandler = shareInlineQueryHandler;
      myRaidService = raidService;
      myIngressClient = ingressClient;
      myDb = db;
      myGeoCoder = geoCoder;
      myClock = clock;
      myTimeZoneService = timeZoneService;
    }

    public bool ProcessLimitQueryString(ref List<VoteLimit> limits, string queryPart)
    {
      if (Regex.Match(queryPart, @"/i(?<limit>\d+)") is { Success: true } match)
      {
        limits ??= new List<VoteLimit>(1);
        limits.Add(new VoteLimit { Vote = VoteEnum.Invitation, Limit = int.Parse(match.Groups["limit"].ValueSpan)});
        return true;
      }

      return false;
    }
    
    public async Task<bool?> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      IReadOnlyCollection<InlineQueryResult> inlineQueryResults;

      Task<Location> location = null;
      async Task<Location> GetLocation() =>  await (location ??= myDb.Set<UserSettings>().GetLocation(data, cancellationToken));
      
      string query = null;
      Portal portal = null;
      bool exRaidGym = false;
      string switchPmParameter = null;
      List<VoteLimit> limits = default;
      foreach (var queryPart in data.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries))
      {
        switch (queryPart)
        {
          case { } when queryPart.StartsWith(GymInlineQueryHandler.PREFIX):
            var guid = queryPart.Substring(GymInlineQueryHandler.PREFIX.Length);
            if (guid.EndsWith('+'))
            {
              guid = guid.Substring(0, guid.Length - 1);
              exRaidGym = true;
            }
            var portalGuid = PortalEx.DecodeGuid(guid);
            portal = await myIngressClient.Get(portalGuid, await GetLocation(), cancellationToken);
            break;
          
          case { } when ProcessLimitQueryString(ref limits, queryPart):
            break;
          
          default:
            query += (query == null ? default(char?) : ' ') + queryPart;
            break;
        }
      }
      if (string.IsNullOrWhiteSpace(data.Query)) // check whole query for sharing branch
      {
        inlineQueryResults = await myShareInlineQueryHandler.GetActivePolls(data.From, cancellationToken);
      }
      else if (string.IsNullOrWhiteSpace(query))
      {
        inlineQueryResults = new[]
        {
          new InlineQueryResultArticle($"EnterPollTopic", "Enter a topic",
            new InputTextMessageContent("Enter a topic to create a poll"))
          {
            Description = "to create a poll",
            ThumbUrl = myUrlHelper.AssetsContent("static_assets/png/POI_Submission_Illustration_02.png").ToString()
          }
        };
      }
      else
      {
        var poll = await new Poll(data) { Title = query, Portal = portal, Limits = limits }
          .DetectRaidTime(myTimeZoneService, GetLocation, async ct => myClock.GetCurrentInstant().InZone(await myGeoCoder.GetTimeZone(data, ct)), cancellationToken);
        var pollId = await myRaidService.GetPollId(poll, data.From, cancellationToken);
        switchPmParameter = portal == null ? $"{SwitchToGymParameter}{pollId}" : null;
        ICollection<VoteEnum> voteFormats = await myDb.Set<Settings>().GetFormats(data.From.Id).ToListAsync(cancellationToken);
        if (voteFormats.Count == 0)
        {
          voteFormats = VoteEnumEx.DefaultVoteFormats;
        }
        inlineQueryResults = voteFormats
            .Select(format => new Poll(poll)
            {
              Id = exRaidGym ? -pollId : pollId,
              AllowedVotes = format,
              ExRaidGym = exRaidGym
            }.InitImplicitVotes(data.From, myBot.BotId))
            .Select((fakePoll, i) => GetInlineResult(fakePoll, i))
            .ToArray();
      }

      await myBot.AnswerInlineQueryWithValidationAsync(data.Id, inlineQueryResults,
        switchPmText: switchPmParameter != null ? "Link the poll to a gym" : null, switchPmParameter: switchPmParameter,
        cacheTime: 0, cancellationToken: cancellationToken);

      await myDb.SaveChangesAsync(cancellationToken);
      return true;
    }

    public InlineQueryResultArticle GetInlineResult(Poll poll, int? suffixNumber = null) =>
      new (poll.GetInlineId(suffixNumber: suffixNumber), poll.GetTitle() + (poll.Id < 0 ? " (EX Raid Gym)" : null), poll.GetMessageText(myUrlHelper, poll.DisableWebPreview()))
      {
        Description = poll.AllowedVotes?.Format(new TextBuilder("Create a poll ")).ToString(),
        HideUrl = true,
        ThumbUrl = poll.GetThumbUrl(myUrlHelper).ToString(),
        ReplyMarkup = poll.GetReplyMarkup()
      };
  }
}