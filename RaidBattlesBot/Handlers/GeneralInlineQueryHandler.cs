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

    public GeneralInlineQueryHandler(ITelegramBotClient bot, IUrlHelper urlHelper)
    {
      myBot = bot;
      myUrlHelper = urlHelper;
    }

    public async Task<bool?> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var query = data.Query;
      if (string.IsNullOrEmpty(query))
        return null;

      InlineQueryResult[] inlineQueryResults = VoteEnumEx.AllowedVoteFormats
        .Select(_ => new Poll
        {
          Title = query,
          AllowedVotes = _
        })
        .Select(fakePoll => new InlineQueryResultArticle
        {
          Id = $"create:{query.GetHashCode()}:{fakePoll.AllowedVotes:D}",
          Title = fakePoll.GetTitle(myUrlHelper),
          Description = fakePoll.AllowedVotes?.Format(new StringBuilder("Создать голосование ")).ToString(),
          HideUrl = true,
          ThumbUrl = fakePoll.GetThumbUrl(myUrlHelper).ToString(),
          InputMessageContent = new InputTextMessageContent
          {
            MessageText = fakePoll.GetMessageText(myUrlHelper, RaidEx.ParseMode).ToString(),
            ParseMode = RaidEx.ParseMode
          },
          ReplyMarkup = fakePoll.GetReplyMarkup()
        }).ToArray();

      return await myBot.AnswerInlineQueryAsync(data.Id, inlineQueryResults, cacheTime: 0, cancellationToken: cancellationToken);
    }
  }
}