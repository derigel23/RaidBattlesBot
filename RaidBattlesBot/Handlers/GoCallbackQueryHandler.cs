using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class GoCallbackQueryHandler : ICallbackQueryHandler
  {
    public const string ID = "go";

    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly ITelegramBotClient myBot;
    private readonly ReplyHandler myReplyHandler;

    public GoCallbackQueryHandler(RaidService raidService, IUrlHelper urlHelper, ITelegramBotClient bot, ReplyHandler replyHandler)
    {
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myBot = bot;
      myReplyHandler = replyHandler;
    }

    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data?.Split(':');
      if (callback?[0] != ID)
        return (null, false, null);

      PollMessage pollMessage;
      if (callback.ElementAtOrDefault(1) is var pollIdSegment && PollEx.TryGetPollId(pollIdSegment, out var pollId, out var format))
      {
        pollMessage = await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { BotId = myBot.BotId, PollId = pollId }, myUrlHelper, format, cancellationToken);
      }
      else
      {
        return ("Poll is publishing. Try later.", true, null);
      }

      if (pollMessage?.Poll is var poll && poll == null)
        return ("Poll is not found", true, null);

      ChatId chatId;
      int? replyToMessageId;
      if (data.Message is { Chat: { } chat, MessageId: var messageId })
      {
        chatId = chat;
        replyToMessageId = messageId;
      }
      else
      {
        chatId = data.From.Id;
        replyToMessageId = null;
      }

      var builder = poll.GetTitle(new TextBuilder("GO "));
      var message = await myBot.SendTextMessageAsync(chatId, builder.ToTextMessageContent(), disableNotification: true, replyToMessageId: replyToMessageId, cancellationToken: cancellationToken);
      await myReplyHandler.ProcessMessage(message, poll, cancellationToken);
      
      return (null, false, null);
    }
  }
}