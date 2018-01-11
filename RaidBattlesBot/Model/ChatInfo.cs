using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Model
{
  public class ChatInfo
  {
    private readonly IMemoryCache myMemoryCache;
    private readonly ITelegramBotClient myBot;

    public ChatInfo(IMemoryCache memoryCache, ITelegramBotClient bot)
    {
      myMemoryCache = memoryCache;
      myBot = bot;
    }

    public async Task<bool> IsAdmin(ChatId chatId, int? userId, CancellationToken cancellationToken = default)
    {
      if (chatId == null) return false;

      if (!(userId is int id)) return false;

      // regular user, admin
      if (chatId.Identifier == id) return true;

      // regular user, not admin
      if (chatId.Identifier > 0) return false;

      // channel/group
      var key = "Admins:" + chatId;
      if (!myMemoryCache.TryGetValue<IImmutableSet<int>>(key, out var chatAdministrators))
      {
        var chatMembers = await myBot.GetChatAdministratorsAsync(chatId, cancellationToken);
        chatAdministrators = chatMembers.Select(_ => _.User.Id).ToImmutableHashSet();
        myMemoryCache.Set(key, chatAdministrators, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5)});
      }

      return chatAdministrators.Contains(id);
    }
  }
}