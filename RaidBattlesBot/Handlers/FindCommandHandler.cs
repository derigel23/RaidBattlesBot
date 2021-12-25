#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Handlers
{
  [BotCommand("find", "Find user by IGN", BotCommandScopeType.AllPrivateChats, Order = 10)]
  public class FindCommandHandler : ReplyBotCommandHandler
  {
    private readonly TelemetryClient myTelemetryClient;
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;
    private readonly IMemoryCache myMemoryCache;

    public FindCommandHandler(TelemetryClient telemetryClient, Message message, RaidBattlesContext context, ITelegramBotClient bot, IMemoryCache memoryCache)
      : base(message)
    {
      myTelemetryClient = telemetryClient;
      myContext = context;
      myBot = bot;
      myMemoryCache = memoryCache;
    }

    // custom timeout, long operations
    private static readonly TimeSpan ourProcessingTimeout = TimeSpan.FromSeconds(90);
    
    protected override async Task<bool?> Handle(Message message, StringSegment text, PollMessage? context = default, CancellationToken parentCancellationToken = default)
    {
      var key = message.Chat.Id + message.MessageId;
      if (!myMemoryCache.TryGetValue(key, out _))
      {
        using var entry = myMemoryCache.CreateEntry(key);
        entry.Value = new object();
      }
      else
      {
        return false; // already processing, this is repeating call from telegram
      }
      
      var builder = new TextBuilder();
      IReplyMarkup? replyMarkup = null;
      var nickname = text.ToString().Trim();
      ChatId chatId = message.Chat!;

      using var cts = new CancellationTokenSource(ourProcessingTimeout);
      var cancellationToken = cts.Token;
      var statusTask = Task.CompletedTask;

      switch (nickname)
      {
        case { Length: > 3 }:

          statusTask = Task.Factory.StartNew(async () =>
            {
              do
              {
                await myBot.SendChatActionAsync(chatId, ChatAction.Typing, cancellationToken);
                await Task.Delay(5000, cancellationToken);
              } while (!cancellationToken.IsCancellationRequested);
            }, cancellationToken, TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning, TaskScheduler.Current)
            .ContinueWith(task =>
            {
              myTelemetryClient.TrackException(task.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted);

          myContext.Database.SetCommandTimeout(ourProcessingTimeout);

          string username = nickname;
          if (nickname.StartsWith('@') is { } byUsername && byUsername)
          {
            username = nickname.Substring(1);
          }
          
          Dictionary<long, string?>? found = null;

          if (!byUsername)
          {
            found = await myContext
              .Set<Player>()
              .Where(player => player.Nickname == nickname)
              .ToDictionaryAsyncLinqToDB(player => player.UserId, player => (string?)player.Nickname, cancellationToken);

            if (found.Count == 0)
            {
              found = (await myContext.Set<Player>().Where(player => player.Nickname.Contains(nickname))
                  .ToListAsyncLinqToDB(cancellationToken))
                .Where(player => player.Nickname.Split(new[] { ',' }).Select(nick => nick.Trim())
                  .Contains(nickname, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(_ => _.UserId, _ => (string?)_.Nickname);
            }
          }

          if (found == null || found.Count == 0)
          {
            found = await (from vote in myContext.Set<Vote>()
              where vote.Username == username
              let rank = Sql.Ext.Rank().Over().PartitionBy(vote.UserId, vote.Username).OrderByDesc(vote.Modified).ToValue()
              where rank == 1
              join player in myContext.Set<Player>() on vote.UserId equals player.UserId into pp
              select new { vote.UserId, player = pp.DefaultIfEmpty().FirstOrDefault() })
            .ToDictionaryAsyncLinqToDB(_ => _.UserId, _ => _.player?.Nickname, cancellationToken);
          }
          
          if (found.Count > 0)
          {
            builder = (await (from vote in myContext.Set<Vote>()
                where found.ContainsKey(vote.UserId)
                let rank = Sql.Ext.Rank().Over().PartitionBy(vote.UserId).OrderByDesc(vote.Modified).ToValue()
                where rank == 1
                select vote)
              .ToListAsyncLinqToDB(cancellationToken))
              .Aggregate(builder, (b, vote) =>
              {
                vote.User.GetLink(b, (u, bb) => UserEx.DefaultUserExtractor(u, bb)
                    .Sanitize(string.IsNullOrEmpty(u.Username) ? null : $" (@{u.Username})"));
                if (found[vote.UserId] is { } storedNickname)
                {
                  b.Append($" {storedNickname:code}");
                }
                return b.NewLine();
              });
          }

          if (builder.Length == 0)
          {
            builder.Sanitize("No one was found.");
          }
          break;
        
        case {Length: > 0}:
          builder.Sanitize("Nick is too short.");
          break;
        
        default:
          builder.Append($"Send user's {"in-game-nick":code} or Telegram {"@username":code} to /find.");
          replyMarkup = new ForceReplyMarkup { InputFieldPlaceholder = "in-game-name or username" };
          break;
      }

      var content = builder.ToTextMessageContent();
      await myBot.SendTextMessageAsync(chatId, content, replyMarkup: replyMarkup, cancellationToken: cancellationToken);

      cts.Cancel();
      await statusTask;

      myMemoryCache.Remove(key);
      
      return false; // processed, but not pollMessage
    }
  }
}