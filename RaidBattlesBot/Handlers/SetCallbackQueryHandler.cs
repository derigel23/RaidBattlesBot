using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = "set")]
  public class SetCallbackQueryHandler : ICallbackQueryHandler<object>
  {
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myTelegramBotClient;
    private readonly ChatInfo myChatInfo;

    public SetCallbackQueryHandler(RaidBattlesContext context, ITelegramBotClient telegramBotClient, ChatInfo chatInfo)
    {
      myContext = context;
      myTelegramBotClient = telegramBotClient;
      myChatInfo = chatInfo;
    }

    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != "set")
        return (null, false, null);

      if (!await myChatInfo.CandEditPoll(data.Message.Chat, data.From?.Id ,cancellationToken))
        return ("У вас недостаточно прав", true, null);

      if (!FlagEnums.TryParseFlags(callback.ElementAtOrDefault(1) ?? "", out VoteEnum allowedVotes, EnumFormat.DecimalValue) || (allowedVotes == VoteEnum.None))
        return (null, false, null);

      var chatId = data.Message.Chat.Id;
      var polls = myContext.Set<Settings>();
      var settings = await polls.FirstOrDefaultAsync(_ => _.Chat == chatId, cancellationToken);
      settings = settings ?? polls.Add(new Settings { Chat = chatId }).Entity;
      settings.DefaultAllowedVotes = allowedVotes;
      await myContext.SaveChangesAsync(cancellationToken);

      await myTelegramBotClient.EditMessageTextAsync(data.Message.Chat, data.Message.MessageId, $"Формат голосования по умолчанию {allowedVotes.Format(new StringBuilder())}",
        replyMarkup: null, cancellationToken: cancellationToken);

      return ("Формат голосования по умолчанию изменён", false, null);
    }
  }
}