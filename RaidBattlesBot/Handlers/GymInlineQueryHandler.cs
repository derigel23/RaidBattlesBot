using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Google.OpenLocationCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Handlers
{
  [InlineQueryHandler(QueryPattern = PATTERN)]
  public class GymInlineQueryHandler : IInlineQueryHandler
  {
    public const string PREFIX = "/gym";
    private const string PATTERN = @"(^|\s+)" + PREFIX + @"(?<pollId>\d*)($|\s+)";
    
    private readonly Update myUpdate;
    private readonly ITelegramBotClient myBot;
    private readonly RaidBattlesContext myDb;
    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly IngressClient myIngressClient;
    
    public GymInlineQueryHandler(Update update, IUrlHelper urlHelper, IngressClient ingressClient, ITelegramBotClient bot, RaidBattlesContext db, RaidService raidService)
    {
      myUpdate = update;
      myUrlHelper = urlHelper;
      myIngressClient = ingressClient;
      myBot = bot;
      myDb = db;
      myRaidService = raidService;
    }

    private const int MAX_PORTALS_PER_RESPONSE = 20;

    public async Task<bool?> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var location = data.Location;
      var queryParts = data.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      
      var poll = default(Poll);
      var pollQuery = new List<string>(queryParts.Length);
      var searchQuery = new List<string>(queryParts.Length);
      var query = pollQuery;
      foreach (var queryPart in queryParts)
      {
        switch (queryPart)
        {
          case string locationPart when OpenLocationCode.IsValidCode(locationPart):
            location = OpenLocationCode.Decode(locationPart) is var code
              ? new Location { Longitude = (float) code.CenterLongitude, Latitude = (float) code.CenterLatitude }
              : location;
            break;

          case string part when Regex.Match(part, PATTERN) is Match match && match.Success:
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
        portals = await myIngressClient.Search(searchQuery, location, cancellationToken);
      }

      var results = new List<InlineQueryResultBase>(Math.Min(portals.Length, MAX_PORTALS_PER_RESPONSE) + 2);

      if ((poll == null) && (pollQuery.Count != 0))
      {
        var voteFormat =
          (await myDb.Set<Settings>().FirstOrDefaultAsync(_ => _.Chat == data.From.Id, cancellationToken))
          ?.DefaultAllowedVotes ?? VoteEnum.Standard;
        var voteFormatOffset = VoteEnumEx.AllowedVoteFormats
                                 .Select((format, i) => format == voteFormat ? i : default(int?))
                                 .FirstOrDefault(_ => _ != null) ?? 0;

        for (var i = 0; i < portals.Length && i < MAX_PORTALS_PER_RESPONSE; i++)
        {
          poll = new Poll(data)
          {
            //Id = pollId + voteFormatOffset,
            Title = string.Join("  ", pollQuery),
            AllowedVotes = voteFormat,
            Portal = portals[i],
            ExRaidGym = false
          };
          poll.Id = await myRaidService.GetPollId(poll, cancellationToken) + voteFormatOffset;

          results.Add(new InlineQueryResultArticle($"create:{poll.Id}", poll.GetTitle(myUrlHelper),
            poll.GetMessageText(myUrlHelper, disableWebPreview: poll.DisableWebPreview()))
            {
              Description = poll.AllowedVotes?.Format(new StringBuilder("Создать голосование ")).ToString(),
              HideUrl = true,
              ThumbUrl = poll.GetThumbUrl(myUrlHelper).ToString(),
              ReplyMarkup = poll.GetReplyMarkup()
            });
          
          if (i == 0)
          {
            poll.Id = -poll.Id;
            poll.ExRaidGym = true;
            results.Add(new InlineQueryResultArticle($"create:{poll.Id}", poll.GetTitle(myUrlHelper) + " (EX Raid Gym)",
              poll.GetMessageText(myUrlHelper, disableWebPreview: poll.DisableWebPreview()))
            {
              Description = poll.AllowedVotes?.Format(new StringBuilder("Создать голосование ")).ToString(),
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
        var title = portal.Name is string name && !string.IsNullOrWhiteSpace(name) ? name : portal.Guid;
        if (portal.EncodeGuid() is string portalGuid)
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
            InlineKeyboardButton.WithSwitchInlineQuery("Создать голосование", $"{PREFIX}{portalGuid} {poll?.Title}")));

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
              InlineKeyboardButton.WithSwitchInlineQuery("Создать голосование ☆ (EX Raid Gym)", $"{PREFIX}{portalGuid}+ {poll?.Title}")));
          }
        }
      }
      
      if (searchQuery.Count == 0)
      {
        results.Add(
          new InlineQueryResultArticle("EnterGymName", "Введите имя гима", new InputTextMessageContent("Введитя имя гима для поиска по имени"))
          {
            Description = "для поиска по имени",
            ThumbUrl = default(Portal).GetImage(myUrlHelper)?.AbsoluteUri
          });
      }

      if (results.Count == 0)
      {
        var search = string.Join(" ", searchQuery);
        results.Add(new InlineQueryResultArticle("NothingFound", "Ничего не найдено", 
          new StringBuilder($"Ничего не найдено по запросу ").Code((builder, mode) => builder.Sanitize(search, mode)).ToTextMessageContent())
        {
          Description = $"Запрос {search}",
          ThumbUrl = myUrlHelper.AssetsContent(@"static_assets/png/btn_close_normal.png").AbsoluteUri
        });
      }
      
      await myBot.AnswerInlineQueryAsync(data.Id, results, cacheTime: 0, isPersonal: true, cancellationToken: cancellationToken);

      myDb.SaveChanges();

      return true;
    }

  }
}