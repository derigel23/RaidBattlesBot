using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot
{
  public class RaidService
  {
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;

    public RaidService(RaidBattlesContext context, ITelegramBotClient bot)
    {
      myContext = context;
      myBot = bot;
    }

    public async Task<bool> AddRaid(string text, PollMessage message, CancellationToken cancellationToken = default)
    {
      Poll poll;
      var raid = new Raid
      {
        Title = text,
        Polls = new List<Poll>
        {
          (poll = new Poll
          {
            Owner = message.UserId,
            Messages = new List<PollMessage> { message }
          })
        }
      };
      myContext.Raids.Add(raid);
      await myContext.SaveChangesAsync(cancellationToken);

      var messageText = poll.GetMessageText().ToString();
      if (message.InlineMesssageId == null)
      {
        var postedMessage = await myBot.SendTextMessageAsync(message.Chat, messageText, ParseMode.Markdown,
          replyMarkup: poll.GetReplyMarkup(), disableNotification: true, cancellationToken: cancellationToken);
        message.Chat = postedMessage.Chat;
        message.MesssageId = postedMessage.MessageId;
      }
      else
      {
        await myBot.EditInlineMessageTextAsync(message.InlineMesssageId, messageText, ParseMode.Markdown,
          replyMarkup: poll.GetReplyMarkup(), cancellationToken: cancellationToken);
      }

      return await myContext.SaveChangesAsync(cancellationToken) > 0;
    }
  }
}