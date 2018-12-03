using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

namespace RaidBattlesBot.Handlers
{
  [InlineQueryHandler]
  public class GeneralInlineQueryHandler : IInlineQueryHandler
  {
    public const string SwitchToGymParameter = "linkToGym";

    private readonly ITelegramBotClient myBot;
    private readonly IUrlHelper myUrlHelper;
    private readonly ShareInlineQueryHandler myShareInlineQueryHandler;
    private readonly RaidService myRaidService;
    private readonly IngressClient myIngressClient;
    private readonly RaidBattlesContext myDb;

    public GeneralInlineQueryHandler(ITelegramBotClient bot, IUrlHelper urlHelper, ShareInlineQueryHandler shareInlineQueryHandler, RaidService raidService, IngressClient ingressClient, RaidBattlesContext db)
    {
      myBot = bot;
      myUrlHelper = urlHelper;
      myShareInlineQueryHandler = shareInlineQueryHandler;
      myRaidService = raidService;
      myIngressClient = ingressClient;
      myDb = db;
    }

    public async Task<bool?> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      IEnumerable<InlineQueryResultBase> inlineQueryResults;

      string query = null;
      Portal portal = null;
      bool exRaidGym = false;
      string switchPmParameter = null;
      foreach (var queryPart in data.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries))
      {
        switch (queryPart)
        {
          case string _ when queryPart.StartsWith(GymInlineQueryHandler.PREFIX):
            var guid = queryPart.Substring(GymInlineQueryHandler.PREFIX.Length);
            if (guid.EndsWith('+'))
            {
              guid = guid.Substring(0, guid.Length - 1);
              exRaidGym = true;
            }
            var portalGuid = PortalEx.DecodeGuid(guid);
            portal = await myIngressClient.Get(portalGuid, data.Location, cancellationToken);
            myDb.Attach(portal);
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
          new InlineQueryResultArticle($"EnterPollTopic", "Введите тему",
            new InputTextMessageContent("Введите тему для создания голосования"))
          {
            Description = "для создания голосования",
            ThumbUrl = myUrlHelper.AssetsContent("static_assets/png/POI_Submission_Illustration_02.png").ToString()
          }
        };
      }
      else
      {
        var pollId = await myRaidService.GetPollId(new Poll(data) { Title = query, Portal = portal }, cancellationToken);
        switchPmParameter = portal == null ? $"{SwitchToGymParameter}{pollId}" : null;
        inlineQueryResults = 
          VoteEnumEx.AllowedVoteFormats
            .Select((_, i) => new Poll
            {
              Id = exRaidGym ? -pollId - i : pollId + i,
              Title = query,
              AllowedVotes = _,
              Portal = portal,
              ExRaidGym = exRaidGym
            })
            .Select(fakePoll => new InlineQueryResultArticle($"create:{fakePoll.Id}", fakePoll.GetTitle(myUrlHelper),
              new InputTextMessageContent(fakePoll.GetMessageText(myUrlHelper, RaidEx.ParseMode).ToString())
              {
                ParseMode = RaidEx.ParseMode,
                DisableWebPagePreview = fakePoll.DisableWebPreview()
              })
              {
                Description = fakePoll.AllowedVotes?.Format(new StringBuilder("Создать голосование ")).ToString(),
                HideUrl = true,
                ThumbUrl = fakePoll.GetThumbUrl(myUrlHelper).ToString(),
                ReplyMarkup = fakePoll.GetReplyMarkup()
            });
      }

      await myBot.AnswerInlineQueryAsync(data.Id, inlineQueryResults,
        switchPmText: switchPmParameter != null ? "Привязать голосование к гиму" : null, switchPmParameter: switchPmParameter,
        cacheTime: 0, cancellationToken: cancellationToken);

      await myDb.SaveChangesAsync(cancellationToken);
      return true;
    }
  }
}