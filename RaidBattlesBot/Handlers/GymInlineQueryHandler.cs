﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Google.OpenLocationCode;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot.Handlers
{
  [InlineQueryHandler(QueryPattern = PATTERN)]
  public class GymInlineQueryHandler : IInlineQueryHandler
  {
    public const string PREFIX = "/gym";
    private const string PATTERN = @"(^|\s+)" + PREFIX + @"(?<pollId>\d*)($|\s+)";

    private readonly ITelegramBotClientEx myBot;
    private readonly RaidBattlesContext myDb;
    private readonly RaidService myRaidService;
    private readonly GeoCoderEx myGeoCoder;
    private readonly IClock myClock;
    private readonly TimeZoneService myTimeZoneService;
    private readonly IUrlHelper myUrlHelper;
    private readonly GeneralInlineQueryHandler myGeneralInlineQueryHandler;
    private readonly IngressClient myIngressClient;
    
    public GymInlineQueryHandler(IUrlHelper urlHelper, GeneralInlineQueryHandler generalInlineQueryHandler, IngressClient ingressClient, ITelegramBotClientEx bot, RaidBattlesContext db, RaidService raidService, GeoCoderEx geoCoder, IClock clock, TimeZoneService timeZoneService)
    {
      myUrlHelper = urlHelper;
      myGeneralInlineQueryHandler = generalInlineQueryHandler;
      myIngressClient = ingressClient;
      myBot = bot;
      myDb = db;
      myRaidService = raidService;
      myGeoCoder = geoCoder;
      myClock = clock;
      myTimeZoneService = timeZoneService;
    }

    private const int MAX_PORTALS_PER_RESPONSE = 20;

    public async Task<bool?> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var location = await myDb.Set<UserSettings>().GetLocation(data, cancellationToken);
      var queryParts = data.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      
      var poll = default(Poll);
      List<VoteLimit> limits = default;
      var pollQuery = new List<string>(queryParts.Length);
      var searchQuery = new List<string>(queryParts.Length);
      var query = pollQuery;
      foreach (var queryPart in queryParts)
      {
        switch (queryPart)
        {
          case { } locationPart when OpenLocationCode.IsValid(locationPart):
            location = OpenLocationCode.Decode(locationPart) is var code
              ? new Location { Longitude = (float) code.CenterLongitude, Latitude = (float) code.CenterLatitude }
              : location;
            break;

          case { } part when Regex.Match(part, PATTERN) is { Success: true } match:
            if (int.TryParse(match.Groups["pollId"].ValueSpan, out var pollId))
            {
              poll = myRaidService
                .GetTemporaryPoll(pollId)
                .InitImplicitVotes(data.From, myBot.BotId);
            }
            query = searchQuery;
            break;

          case { } part when myGeneralInlineQueryHandler.ProcessLimitQueryString(ref limits, part):
            break;
          
          default:
            query.Add(queryPart);
            break;
        }
      }

      Portal[] portals;
      if (searchQuery.Count == 0)
      {
        portals = await myIngressClient.GetPortals(0.200, location, cancellationToken);
      }
      else
      {
        portals = await myIngressClient.Search(searchQuery, location, near: true, cancellationToken);
      }

      var results = new List<InlineQueryResult>(Math.Min(portals.Length, MAX_PORTALS_PER_RESPONSE) + 2);

      if (poll == null && pollQuery.Count != 0)
      {
        var voteFormat = await myDb.Set<Settings>().GetFormat(data.From.Id, cancellationToken);
        poll = await new Poll(data)
        {
          Title = string.Join("  ", pollQuery),
          AllowedVotes = voteFormat,
          ExRaidGym = false,
          Limits = limits
        }.DetectRaidTime(myTimeZoneService, () => Task.FromResult(location), async ct => myClock.GetCurrentInstant().InZone(await myGeoCoder.GetTimeZone(data, ct)), cancellationToken);
        
        for (var i = 0; i < portals.Length && i < MAX_PORTALS_PER_RESPONSE; i++)
        {
          var portalPoll = new Poll(poll)
          {
            Portal = portals[i],
            Limits = poll.Limits ?? limits
          }.InitImplicitVotes(data.From, myBot.BotId);
          await myRaidService.GetPollId(portalPoll, data.From, cancellationToken);

          results.Add(myGeneralInlineQueryHandler.GetInlineResult(portalPoll));
          
          if (i == 0)
          {
            portalPoll.Id = -portalPoll.Id;
            portalPoll.ExRaidGym = true;
            results.Add(myGeneralInlineQueryHandler.GetInlineResult(portalPoll));
          }
        }
      }
      else for (var i = 0; i < portals.Length && i < MAX_PORTALS_PER_RESPONSE; i++)
      {
        var portal = portals[i];
        var title = portal.Name is { } name && !string.IsNullOrWhiteSpace(name) ? name : portal.Guid;
        if (portal.EncodeGuid() is { } portalGuid)
        {
          InlineQueryResultArticle Init(InlineQueryResultArticle article, InlineKeyboardButton createButton)
          {
            const int thumbnailSize = 64;
            article.Description = portal.Address;
            article.ReplyMarkup = new InlineKeyboardMarkup(createButton);
            article.ThumbUrl = portal.GetImage(myUrlHelper, thumbnailSize)?.AbsoluteUri;
            article.ThumbHeight = thumbnailSize;
            article.ThumbWidth = thumbnailSize;
            return article;
          }

          var portalContent = new TextBuilder()
            .Bold(builder => builder.Sanitize(portal.Name)).NewLine()
            .Sanitize(portal.Address)
            .Link("\u200B", portal.Image)
            .ToTextMessageContent();
          results.Add(Init(
            new InlineQueryResultArticle($"portal:{portal.Guid}", title, portalContent),
            InlineKeyboardButton.WithSwitchInlineQuery("Create a poll", $"{PREFIX}{portalGuid} {poll?.Title}")));

          if (i == 0)
          {
            var exRaidPortalContent = new TextBuilder()
              .Sanitize("☆ ")
              .Bold(builder => builder.Sanitize(portal.Name))
              .Sanitize(" (EX Raid Gym)").NewLine()
              .Sanitize(portal.Address)
              .Link("\u200B", portal.Image)
              .ToTextMessageContent();
            results.Add(Init(
              new InlineQueryResultArticle($"portal:{portal.Guid}+", $"☆ {title} (EX Raid Gym)", exRaidPortalContent),
              InlineKeyboardButton.WithSwitchInlineQuery("Create a poll ☆ (EX Raid Gym)", $"{PREFIX}{portalGuid}+ {poll?.Title}")));
          }
        }
      }
      
      if (searchQuery.Count == 0)
      {
        results.Add(
          new InlineQueryResultArticle("EnterGymName", "Enter a Gym's Title", new InputTextMessageContent("Enter a Gym's Title to search"))
          {
            Description = "to search",
            ThumbUrl = default(Portal).GetImage(myUrlHelper)?.AbsoluteUri
          });
      }

      if (results.Count == 0)
      {
        var search = string.Join(" ", searchQuery);
        results.Add(new InlineQueryResultArticle("NothingFound", "Nothing found", 
          new TextBuilder($"Nothing found by request ").Code(builder => builder.Sanitize(search)).ToTextMessageContent())
        {
          Description = $"Request {search}",
          ThumbUrl = myUrlHelper.AssetsContent(@"static_assets/png/btn_close_normal.png").AbsoluteUri
        });
      }
      
      await myBot.AnswerInlineQueryWithValidationAsync(data.Id, results, cacheTime: 0, isPersonal: true, cancellationToken: cancellationToken);

      await myDb.SaveChangesAsync(cancellationToken);

      return true;
    }

  }
}