using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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

    public async Task<ChatMemberStatus> GetChatMemberStatus([CanBeNull] ChatId chat, long? userId, CancellationToken cancellation = default)
    {
      if (!(chat?.Identifier is long chatId) || chatId == 0)
        return ChatMemberStatus.Left;

      if (!(userId is long id))
        return ChatMemberStatus.Left;

      // regular user, itself
      if (chatId == id)
        return ChatMemberStatus.Creator;

      // regular user, not itself
      if ((chatId > 0) || (userId < 0))
        return ChatMemberStatus.Restricted;

      var key = (chatId, id: (int)id);
      return await myMemoryCache.GetOrCreateAsync(key, async entry =>
      {
        entry.SlidingExpiration = TimeSpan.FromMinutes(5);
        return (await myBot.GetChatMemberAsync(key.chatId, key.id, cancellation)).Status;
      });
    }
  }
}