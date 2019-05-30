using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

using Poll = RaidBattlesBot.Model.Poll;

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
        ICollection<VoteEnum> voteFormats = await myDb.Set<Settings>().GetFormats(data.From.Id, cancellationToken).ToListAsync(cancellationToken);
        if (voteFormats.Count == 0)
        {
          voteFormats = VoteEnumEx.DefaultVoteFormats;
        }
        inlineQueryResults = voteFormats
            .Select(format => new Poll
            {
              Id = exRaidGym ? -pollId : pollId,
              Title = query,
              AllowedVotes = format,
              Portal = portal,
              ExRaidGym = exRaidGym
            })
            .Select(fakePoll => new InlineQueryResultArticle(fakePoll.GetInlineId(), fakePoll.GetTitle(myUrlHelper),
              fakePoll.GetMessageText(myUrlHelper, disableWebPreview: fakePoll.DisableWebPreview()))
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

      myDb.SaveChanges();
      return true;
    }
  }
}