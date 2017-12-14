using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType.TextMessage)]
  public class TextMessageHandler : IMessageHandler
  {
    private readonly ITelegramBotClient myTelegramBotClient;
    private readonly Message myMessage;

    public TextMessageHandler(ITelegramBotClient telegramBotClient, Message message)
    {
      myTelegramBotClient = telegramBotClient;
      myMessage = message;
    }

    public async Task<bool> Handle(Message message, object context = default , CancellationToken cancellationToken = default)
    {
      IReplyMarkup replyMarkup = new InlineKeyboardMarkup(new[]
      {
        new InlineKeyboardButton[]
        {
          new InlineKeyboardCallbackButton("❤", "red"),
          new InlineKeyboardCallbackButton("💛", "yellow"),
          new InlineKeyboardCallbackButton("💙", "blue"),
        },
        new InlineKeyboardButton[]
        {
          new InlineKeyboardCallbackButton("♡", "blue"),
          new InlineKeyboardCallbackButton("🌐", "globe"),
          new InlineKeyboardSwitchInlineQueryButton("↺", "/r"),
        }
      });

      var reply = myTelegramBotClient.SendTextMessageAsync(myMessage.Chat.Id, "Boo", ParseMode.Markdown,
        replyMarkup: replyMarkup, replyToMessageId: myMessage.MessageId, disableNotification: true, cancellationToken: cancellationToken);
      
      return reply != null;
    }

    // TODO: HACK
    public static readonly string ProcessFurther = Guid.NewGuid().ToString();
  }
}