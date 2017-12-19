using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;

namespace RaidBattlesBot.Handlers
{
  [InlineQueryHandler(QueryPrefix = "share")]
  public class ShareInlineQueryHandler : IInlineQueryHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;

    public ShareInlineQueryHandler(RaidBattlesContext context, ITelegramBotClient bot)
    {
      myContext = context;
      myBot = bot;
    }

    public async Task<bool> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var queryParts = data.Query.Split(':');
      if (queryParts[0] != "share")
        return false;

      if (!int.TryParse(queryParts.ElementAtOrDefault(1) ?? "", out var pollid))
        return false;

      var poll = await myContext.Polls
        .Where(_ => _.Id == pollid)
        .Include(_ => _.Raid)
        .Include(_ => _.Votes)
        .FirstOrDefaultAsync(cancellationToken);

      InlineQueryResult[] inlineQueryResults;
      if (poll == null)
      {
        inlineQueryResults = new InlineQueryResult[0];
      }
      else
      {
        var title = poll.Raid.Title;
        inlineQueryResults = new InlineQueryResult[]
        {
          new InlineQueryResultArticle
          {
            Id = $"poll:{poll.Id}",
            Title = title,
            Description = "Склонировать голосование",
            //Url = "https://static-maps.yandex.ru/1.x/?l=map&ll=37.626187,55.741424&pt=37.618977,55.744091,pm2ntl",
            HideUrl = true,
            //ThumbUrl = "http://json.e2e2.ru/r/absol.png",
            InputMessageContent = new InputTextMessageContent { MessageText = poll.GetMessageText().ToString(), ParseMode = ParseMode.Markdown },
            ReplyMarkup = poll.GetReplyMarkup()
          },
        };
      }

      return await myBot.AnswerInlineQueryAsync(data.Id, inlineQueryResults, cacheTime: 0, cancellationToken: cancellationToken);
    }
  }
}