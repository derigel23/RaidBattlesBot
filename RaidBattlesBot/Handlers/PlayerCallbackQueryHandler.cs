using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class PlayerCallbackQueryHandler : ICallbackQueryHandler
  {
    public const string ID = "player";

    private readonly RaidBattlesContext myDb;

    public PlayerCallbackQueryHandler(RaidBattlesContext db)
    {
      myDb = db;
    }
    
    public async Task<(string text, bool showAlert, string url)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != ID)
        return (null, false, null);

      switch (callback.Skip(1).FirstOrDefault()?.ToLowerInvariant())
      {
        case "clear":
          if (await myDb.Set<Player>().Where(p => p.UserId == data.From.Id).FirstOrDefaultAsync(cancellationToken) is {} player)
          {
            myDb.Set<Player>().Remove(player);
            await myDb.SaveChangesAsync(cancellationToken);
          }
          return ("Your IGN is removed", false, null);
      }
      
      return (null, false, null);
    }
  }
}