using System;
using System.Collections.Generic;
using System.Text;
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
    private readonly IDateTimeZoneProvider myDateTimeZoneProvider;
    private readonly IUrlHelper myUrlHelper;
    private readonly IngressClient myIngressClient;
    
    public GymInlineQueryHandler(IUrlHelper urlHelper, IngressClient ingressClient, ITelegramBotClientEx bot, RaidBattlesContext db, RaidService raidService, GeoCoderEx geoCoder, IClock clock, IDateTimeZoneProvider dateTimeZoneProvider)
    {
      myUrlHelper = urlHelper;
      myIngressClient = ingressClient;
      myBot = bot;
      myDb = db;
      myRaidService = raidService;
      myGeoCoder = geoCoder;
      myClock = clock;
      myDateTimeZoneProvider = dateTimeZoneProvider;
    }

    private const int MAX_PORTALS_PER_RESPONSE = 20;

    public async Task<bool?> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var location = await myDb.Set<UserSettings>().GetLocation(data, cancellationToken);
      var queryParts = data.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      
      var poll = default(Poll);
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
            if (int.TryParse(match.Groups["pollId"].Value, out var pollId))
            {
              poll = myRaidService.GetTemporaryPoll(pollId);
            }
            query = searchQuery;

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

      if ((poll == null) && (pollQuery.Count != 0))
      {
        var voteFormat = await myDb.Set<Settings>().GetFormat(data.From.Id, cancellationToken);
        poll = await new Poll(data)
        {
          Title = string.Join("  ", pollQuery),
          AllowedVotes = voteFormat,
          ExRaidGym = false
        }.DetectRaidTime(myDateTimeZoneProvider, async ct => myClock.GetCurrentInstant().InZone(await myGeoCoder.GetTimeZone(data, ct)), cancellationToken);
        
        for (var i = 0; i < portals.Length && i < MAX_PORTALS_PER_RESPONSE; i++)
        {
          poll = new Poll(poll)
          {
            Portal = portals[i]
          };
          await myRaidService.GetPollId(poll, cancellationToken);

          results.Add(new InlineQueryResultArticle(poll.GetInlineId(), poll.GetTitle(),
            poll.GetMessageText(myUrlHelper, disableWebPreview: poll.DisableWebPreview()))
            {
              Description = poll.AllowedVotes?.Format(new StringBuilder("Create a poll ")).ToString(),
              HideUrl = true,
              ThumbUrl = poll.GetThumbUrl(myUrlHelper).ToString(),
              ReplyMarkup = poll.GetReplyMarkup()
            });
          
          if (i == 0)
          {
            poll.Id = -poll.Id;
            poll.ExRaidGym = true;
            results.Add(new InlineQueryResultArticle(poll.GetInlineId(), poll.GetTitle() + " (EX Raid Gym)",
              poll.GetMessageText(myUrlHelper, disableWebPreview: poll.DisableWebPreview()))
            {
              Description = poll.AllowedVotes?.Format(new StringBuilder("Create a poll ")).ToString(),
              HideUrl = true,
              ThumbUrl = poll.GetThumbUrl(myUrlHelper).ToString(),
              ReplyMarkup = poll.GetReplyMarkup()
            });
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

          var portalContent = new StringBuilder()
            .Bold((builder, mode) => builder.Sanitize(portal.Name)).NewLine()
            .Sanitize(portal.Address)
            .Link("\u200B", portal.Image)
            .ToTextMessageContent();
          results.Add(Init(
            new InlineQueryResultArticle($"portal:{portal.Guid}", title, portalContent),
            InlineKeyboardButton.WithSwitchInlineQuery("Create a poll", $"{PREFIX}{portalGuid} {poll?.Title}")));

          if (i == 0)
          {
            var exRaidPortalContent = new StringBuilder()
              .Sanitize("☆ ")
              .Bold((builder, mode) => builder.Sanitize(portal.Name))
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
          new StringBuilder($"Nothing found by request ").Code((builder, mode) => builder.Sanitize(search, mode)).ToTextMessageContent())
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