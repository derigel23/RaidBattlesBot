using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Google.OpenLocationCode;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Migrations;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Handlers
{
  [InlineQueryHandler(QueryPattern = PATTERN)]
  public class GymInlineQueryHandler : IInlineQueryHandler
  {
    public const string PREFIX = "/gym";
    private const string PATTERN = "^/gym(?<pollId>\\d*)(\\s+.*)?$";
    
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

    private const int MAX_PORTALS_AROUND = 5;
    private const int MAX_PORTALS_PER_RESPONSE = 20;

    public async Task<bool?> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var location = data.Location;
      string pollQuery = null;
      if (Regex.Match(data.Query, PATTERN) is Match match && match.Success && int.TryParse(match.Groups["pollId"].Value, out var pollId))
      {
        pollQuery = myRaidService.GetTemporaryPoll(pollId)?.Title;
      }
      string searchQuery = null;
      foreach (var queryPart in data.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1))
      {
        switch (queryPart)
        {
          case string locationPart when OpenLocationCode.IsValidCode(locationPart):
            location = OpenLocationCode.Decode(locationPart) is var code
              ? new Location { Longitude = (float) code.CenterLongitude, Latitude = (float) code.CenterLatitude }
              : location;
            break;

          default:
            searchQuery += (searchQuery == null ? default(char?) : ' ') + queryPart;
            break;
        }
      }

      IList<Portal> portals;
      if (string.IsNullOrWhiteSpace(searchQuery))
      {
        var localPortals = await myIngressClient.GetPortals(0.200, location, cancellationToken);
        portals = new List<Portal>(Math.Max(localPortals.Length, MAX_PORTALS_AROUND));
        for (var i = 0; i < localPortals.Length && i < MAX_PORTALS_AROUND; i++)
        {
          portals.Add(await myIngressClient.Get(localPortals[i].Guid, location, cancellationToken));
        }
      }
      else
      {
        portals = await myIngressClient.Search(searchQuery, location, cancellationToken);
      }

      var results = new List<InlineQueryResultBase>(portals.Count + 2);
      for (var i = 0; i < portals.Count && i < MAX_PORTALS_PER_RESPONSE; i++)
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
            article.ThumbUrl = portal.GetImage(myUrlHelper, thumbnailSize).AbsoluteUri;
            article.ThumbHeight = thumbnailSize;
            article.ThumbWidth = thumbnailSize;
            return article;
          }

          results.Add(Init(
            new InlineQueryResultArticle($"portal:{portal.Guid}", title,
              new InputTextMessageContent($"*{portal.Name}*\n{portal.Address}[\u200B]({portal.Image})[\u200B]()") { ParseMode = ParseMode.Markdown }),
            InlineKeyboardButton.WithSwitchInlineQuery("Создать голосование", $"{PREFIX}{portalGuid} {pollQuery}")));

          if (i == 0)
          {
            results.Add(Init(
              new InlineQueryResultArticle($"portal:{portal.Guid}+", $"☆ {title} (EX Raid Gym)",
                new InputTextMessageContent($"☆ *{portal.Name}* (EX Raid Gym)\n{portal.Address}[\u200B]({portal.Image})[\u200B]()") { ParseMode = ParseMode.Markdown }),
              InlineKeyboardButton.WithSwitchInlineQuery("Создать голосование ☆ (EX Raid Gym)", $"{PREFIX}{portalGuid}+ {pollQuery}")));
          }
        }
      }
      
      if (string.IsNullOrWhiteSpace(searchQuery))
      {
        results.Add(
          new InlineQueryResultArticle("EnterGymName", "Введите имя гима", new InputTextMessageContent("Введитя имя гима для поиска по имени"))
          {
            Description = "для поиска по имени",
            ThumbUrl = default(Portal).GetImage(myUrlHelper).AbsoluteUri
          });
      }

      if (results.Count == 0)
      {
        results.Add(new InlineQueryResultArticle("NothingFound", "Ничего не найдено", new InputTextMessageContent($"Ничего не найдено по запросу `{searchQuery}`") { ParseMode = ParseMode.Markdown })
        {
          Description = $"Запрос {searchQuery}",
          ThumbUrl = myUrlHelper.AssetsContent(@"static_assets/png/btn_close_normal.png").AbsoluteUri
        });
      }
      
      await myBot.AnswerInlineQueryAsync(data.Id, results, cacheTime: 0, isPersonal: true, cancellationToken: cancellationToken);

      await myDb.SaveChangesAsync(cancellationToken);

      return true;
    }

  }
}