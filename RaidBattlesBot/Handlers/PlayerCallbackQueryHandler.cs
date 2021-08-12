using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class PlayerCallbackQueryHandler : ICallbackQueryHandler
  {
    public const string ID = "player";

    public static class Commands
    {
      public const string ClearIGN = "clear";
      public const string ClearFriendCode = "clear_fc";
    }
    
    private readonly RaidBattlesContext myDb;
    private readonly ITelegramBotClient myBot;

    public PlayerCallbackQueryHandler(RaidBattlesContext db, ITelegramBotClient bot)
    {
      myDb = db;
      myBot = bot;
    }
    
    public async Task<(string text, bool showAlert, string url)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data?.Split(':');
      if (callback?[0] != ID)
        return (null, false, null);

      var player = await myDb.Set<Player>().FirstOrDefaultAsync(p => p.UserId == data.From.Id, cancellationToken);
      
      switch (callback.Skip(1).FirstOrDefault()?.ToLowerInvariant())
      {
        case Commands.ClearIGN:
          if (player != null)
          {
            player.Nickname = null;
            await myDb.SaveChangesAsync(cancellationToken);
          }

          await myBot.EditMessageReplyMarkupAsync(data, InlineKeyboardMarkup.Empty(), cancellationToken);
          return ("Your IGN is removed", false, null);

        case Commands.ClearFriendCode:
          if (player != null)
          {
            player.FriendCode = null;
            await myDb.SaveChangesAsync(cancellationToken);
          }
          
          await myBot.EditMessageReplyMarkupAsync(data, InlineKeyboardMarkup.Empty(), cancellationToken);
          return ("Your Friend Code is removed", false, null);
      }
      
      return (null, false, null);
    }
  }
}