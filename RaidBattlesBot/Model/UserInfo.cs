using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Model
{
  public class UserInfo
  {
    private readonly IMemoryCache myMemoryCache;
    private readonly ITelegramBotClient myBot;
    private readonly TelemetryClient myTelemetryClient;

    public UserInfo(IMemoryCache memoryCache, ITelegramBotClient bot, TelemetryClient telemetryClient)
    {
      myMemoryCache = memoryCache;
      myBot = bot;
      myTelemetryClient = telemetryClient;
    }

    private static readonly Task<bool> IsUserAllowedTrue = Task.FromResult(true);

    public Task<bool> IsUserAllowed(int userId, CancellationToken cancellationToken = default)
    {
      return IsUserAllowedTrue;
      return myMemoryCache.GetOrCreateAsync($"user:{userId}", async entry =>
      {
        entry.SetSlidingExpiration(TimeSpan.FromMinutes(10));
        try
        {
          //await myBot.SendChatActionAsync(userId, ChatAction.Typing, cancellationToken);
          return true;
        }
        catch (ApiRequestException ex)
        {
          myTelemetryClient.TrackException(ex, new Dictionary<string, string>
          {
            { "userId", userId.ToString() }
          });
          return false;
        }
      });
    }
  }
}