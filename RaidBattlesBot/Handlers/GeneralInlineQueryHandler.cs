using System.Threading;
using System.Threading.Tasks;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;

namespace RaidBattlesBot.Handlers
{
  [InlineQueryHandler]
  public class GeneralInlineQueryHandler : IInlineQueryHandler
  {
    private readonly ITelegramBotClient myBot;

    public GeneralInlineQueryHandler(ITelegramBotClient bot)
    {
      myBot = bot;
    }

    public async Task<bool> Handle(InlineQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var query = data.Query;
      if (string.IsNullOrEmpty(query))
        return false;

      var fakePoll = new Poll
      {
        Raid = new Raid
        {
          Title = query
        }
      };

      var inlineQueryResults = new InlineQueryResult[]
      {
        new InlineQueryResultArticle
        {
          Id = $"create:{query.GetHashCode()}",
          Title = query,
          Description = "Создать голосование",
          //Url = "https://static-maps.yandex.ru/1.x/?l=map&ll=37.626187,55.741424&pt=37.618977,55.744091,pm2ntl",
          HideUrl = true,
          //ThumbUrl = "http://json.e2e2.ru/r/absol.png",
          InputMessageContent = new InputTextMessageContent
          {
            MessageText = fakePoll.GetMessageText().ToString(),
            ParseMode = ParseMode.Markdown
          },
          ReplyMarkup = fakePoll.GetReplyMarkup()
        },
      };

      return await myBot.AnswerInlineQueryAsync(data.Id, inlineQueryResults, cacheTime: 0, cancellationToken: cancellationToken);
    }
  }
}