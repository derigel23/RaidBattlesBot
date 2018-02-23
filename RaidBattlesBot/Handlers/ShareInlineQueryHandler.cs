using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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

    public ShareInlineQueryHandler(RaidBattlesContext context, ITelegramBotClient bot, IUrlHelper urlHelper, UserInfo userInfo)
    {
      myContext = context;
      myBot = bot;
      myUrlHelper = urlHelper;
      myUserInfo = userInfo;
    }

    public async Task<bool?> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var queryParts = data.Query.Split(':');
      if (queryParts[0] != "share")
        return null;

      if (!int.TryParse(queryParts.ElementAtOrDefault(1) ?? "", out var pollid))
        return false;

      var poll = await myContext.Polls
        .Where(_ => _.Id == pollid)
        .IncludeRelatedData()
        .FirstOrDefaultAsync(cancellationToken);

      var inlineQueryResults = new List<InlineQueryResult>();
      if (poll != null)
      {
        inlineQueryResults.Add(
          new InlineQueryResultArticle
          {
            Id = $"poll:{poll.Id}",
            Title = poll.GetTitle(myUrlHelper),
            Description = "Клонировать голосование",
            HideUrl = true,
            ThumbUrl = poll.GetThumbUrl(myUrlHelper).ToString(),
            InputMessageContent = new InputTextMessageContent
            {
              MessageText = (await poll.GetMessageText(myUrlHelper, myUserInfo, RaidEx.ParseMode, cancellationToken)).ToString(),
              ParseMode = RaidEx.ParseMode
            },
            ReplyMarkup = poll.GetReplyMarkup()
          });

        if (poll.Raid() is Raid raid)
        {
          inlineQueryResults.Add(
            new InlineQueryResultVenue
            {
              Id = $"location:{raid.Id}",
              Title = raid.Title,
              Address = "Запостить локу",
              Latitude = (float)raid.Lat,
              Longitude = (float)raid.Lon,
              ThumbUrl = myUrlHelper.AssetsContent("static_assets/png/ic_map.png").ToString(),
              InputMessageContent = new InputVenueMessageContentNew
              {
                Name = raid.Title,
                Address = RaidEx.Delimeter.JoinNonEmpty(raid.Gym ?? raid.PossibleGym, raid.Description),
                Latitude = (float)raid.Lat,
                Longitude = (float)raid.Lon,
              },
            });
        }
      }

      return await myBot.AnswerInlineQueryAsync(data.Id, inlineQueryResults.ToArray(), cacheTime: 0, cancellationToken: cancellationToken);
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