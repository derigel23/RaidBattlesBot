using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

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

    public Task<bool> IsUserAllowed(User user, CancellationToken cancellationToken = default)
    {
      return IsUserAllowedTrue;
      return myMemoryCache.GetOrCreateAsync($"user:{user.Id}", async entry =>
      {
        entry.SetSlidingExpiration(TimeSpan.FromMinutes(10));
        try
        {
          await myBot.GetUserProfilePhotosAsync(user.Id, 0, 0, cancellationToken);
          return true;
        }
        catch (ApiRequestException ex)
        {
          myTelemetryClient.TrackDependency(nameof(UserInfo), user.Id.ToString(), $"{nameof(myBot.GetUserProfilePhotosAsync)}:{user.Id}", JsonConvert.SerializeObject(user), DateTimeOffset.MinValue, TimeSpan.Zero, ex.Message, false);
          return false;
        }
      });
    }
  }
}