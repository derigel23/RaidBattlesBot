using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;

namespace RaidBattlesBot.Handlers
{
  [InlineQueryHandler]
  public class GeneralInlineQueryHandler : IInlineQueryHandler
  {
    private readonly ITelegramBotClient myBot;
    private readonly IUrlHelper myUrlHelper;
    private readonly UserInfo myUserInfo;
    private readonly ShareInlineQueryHandler myShareInlineQueryHandler;

    public GeneralInlineQueryHandler(ITelegramBotClient bot, IUrlHelper urlHelper, UserInfo userInfo, ShareInlineQueryHandler shareInlineQueryHandler)
    {
      myBot = bot;
      myUrlHelper = urlHelper;
      myUserInfo = userInfo;
      myShareInlineQueryHandler = shareInlineQueryHandler;
    }

    public async Task<bool?> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      InlineQueryResult[] inlineQueryResults;

      var query = data.Query;
      if (string.IsNullOrWhiteSpace(query))
      {
        inlineQueryResults = await myShareInlineQueryHandler.GetActivePolls(data.From, cancellationToken);
      }
      else
      {
        inlineQueryResults = await
          Task.WhenAll(VoteEnumEx.AllowedVoteFormats
          .Select(_ => new Poll
          {
            Title = query,
            AllowedVotes = _
          })
          .Select(async fakePoll => new InlineQueryResultArticle
          {
            Id = $"create:{query.GetHashCode()}:{fakePoll.AllowedVotes:D}",
            Title = fakePoll.GetTitle(myUrlHelper),
            Description = fakePoll.AllowedVotes?.Format(new StringBuilder("Создать голосование ")).ToString(),
            HideUrl = true,
            ThumbUrl = fakePoll.GetThumbUrl(myUrlHelper).ToString(),
            InputMessageContent = new InputTextMessageContent
            {
              MessageText = (await fakePoll.GetMessageText(myUrlHelper, myUserInfo, RaidEx.ParseMode, cancellationToken)).ToString(),
              ParseMode = RaidEx.ParseMode,
              DisableWebPagePreview = fakePoll.GetRaidId() == null
            },
            ReplyMarkup = fakePoll.GetReplyMarkup()
          }));
      }

      return await myBot.AnswerInlineQueryAsync(data.Id, inlineQueryResults, cacheTime: 0, cancellationToken: cancellationToken);
    }
  }
}