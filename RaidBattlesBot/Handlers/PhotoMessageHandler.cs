using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MessageType(MessageType.PhotoMessage)]
  public class PhotoMessageHandler : IMessageHandler
  {
    public async Task<bool> Handle(Message data, object context = default, CancellationToken cancellationToken = default)
    {
      //            var markup = new InlineKeyboardMarkup(new InlineKeyboardButton[]
      //            {
      ////              new InlineKeyboardSwitchInlineQueryCurrentChatButton("Location", message.MessageId.ToString()),
      //              new InlineKeyboardSwitchInlineQueryButton("Location2", message.MessageId.ToString()),
      ////              new InlineKeyboardCallbackButton("Location3", message.MessageId.ToString()), 
      //            });
      //            await myBot.SendTextMessageAsync(message.Chat, "[aaa](http://json.e2e2.ru/?lat=55.759243&lon=37.557481&b=Charmeleon&t=18:20:48)",
      //              replyToMessageId: message.MessageId, parseMode: ParseMode.Markdown, replyMarkup: markup,
      //              disableNotification: true, cancellationToken: cancellationToken);

      return false;
    }
  }
}