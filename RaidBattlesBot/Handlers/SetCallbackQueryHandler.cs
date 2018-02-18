using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = "set")]
  public class SetCallbackQueryHandler : ICallbackQueryHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myTelegramBotClient;
    private readonly User myBot;

    public SetCallbackQueryHandler(RaidBattlesContext context, ITelegramBotClient telegramBotClient, User bot)
    {
      myContext = context;
      myTelegramBotClient = telegramBotClient;
      myBot = bot;
    }

    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != "set")
        return (null, false, null);

      if (!FlagEnums.TryParseFlags(callback.ElementAtOrDefault(1) ?? "", out VoteEnum allowedVotes, EnumFormat.DecimalValue) || (allowedVotes == VoteEnum.None))
        return (null, false, null);

      var chatId = data.Message.Chat.Id;
      var settings = await myContext.Settings.FirstOrDefaultAsync(_ => _.Chat == chatId, cancellationToken);
      settings = settings ?? myContext.Settings.Add(new Settings { Chat = chatId }).Entity;
      settings.DefaultAllowedVotes = allowedVotes;
      await myContext.SaveChangesAsync(cancellationToken);

      await myTelegramBotClient.EditMessageTextAsync(data.Message.Chat, data.Message.MessageId, $"Формат голосования по умолчанию {allowedVotes.Format(new StringBuilder())}",
        replyMarkup: null, cancellationToken: cancellationToken);

      return ("Формат голосования по умолчанию изменён", false, null);
    }
  }
}