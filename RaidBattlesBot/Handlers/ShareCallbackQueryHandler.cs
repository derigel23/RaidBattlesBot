using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = "share")]
  public class ShareCallbackQueryHandler : ICallbackQueryHandler
  {
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;

    public ShareCallbackQueryHandler(RaidBattlesContext context, ITelegramBotClient bot)
    {
      myContext = context;
      myBot = bot;
    }

    public async Task<string> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != "share")
        return null;
      
      if (!int.TryParse(callback.ElementAtOrDefault(1) ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out var pollId))
        return "Голование подготавливается. Повторите позже"; ;

      return null;
    }
  }
}