using System.Threading;
using System.Threading.Tasks;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = PREFIX)]
  public class GymCallbackQueryHandler : ICallbackQueryHandler
  {
    private readonly ITelegramBotClient myTelegramBotClient;
    public const string PREFIX = "gym";

    public GymCallbackQueryHandler(ITelegramBotClient telegramBotClient)
    {
      myTelegramBotClient = telegramBotClient;
    }
    
    public async Task<(string text, bool showAlert, string url)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var query = data.Data.Substring(PREFIX.Length);
      var markup = new InlineKeyboardMarkup(
        InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Выберите гим", $"{GymInlineQueryHandler.PREFIX}{query} "));
      
      if (data.InlineMessageId is string inlineMessageId)
      {
        await myTelegramBotClient.EditMessageReplyMarkupAsync(inlineMessageId, markup, cancellationToken);
      }
      else
      {
        await myTelegramBotClient.EditMessageReplyMarkupAsync(data.Message.Chat, data.Message.MessageId, markup, cancellationToken);
      }
      return ("Выберите гим", false, null);
    }
  }
}