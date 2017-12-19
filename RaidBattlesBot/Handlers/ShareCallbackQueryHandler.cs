using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Handlers;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
      
      if (!int.TryParse(callback.ElementAt(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var pollId))
        return null;

      return null;
    }
  }
}