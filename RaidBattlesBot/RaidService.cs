﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelegateDecompiler.EntityFrameworkCore;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Team23.TelegramSkeleton;
using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot
{
  [UsedImplicitly]
  public class RaidService
  {
    private readonly RaidBattlesContext myContext;
    private readonly Func<long?, ITelegramBotClient> myBot;
    private readonly TelemetryClient myTelemetryClient;
    private readonly Func<long?, ChatInfo> myChatInfo;
    private readonly IMemoryCache myMemoryCache;
    private readonly long? myLogChat;

    public RaidService(RaidBattlesContext context, IDictionary<long, ITelegramBotClient> bots, TelemetryClient telemetryClient, IMemoryCache memoryCache, Func<ITelegramBotClient, ChatInfo> chatInfo, IOptions<BotConfiguration> botOptions)
    {
      myContext = context;
      myTelemetryClient = telemetryClient;
      myMemoryCache = memoryCache;
      myLogChat = botOptions.Value?.LogChatId is { } chatId && chatId != default ? chatId : default(long?);

      var fallbackBot = botOptions.Value?.DefaultBotId is {} defaultBotId ? bots[defaultBotId] : bots.Values.First(); // at least one bot, default
      myBot = botId => botId.HasValue && bots.TryGetValue(botId.Value, out var bot) ? bot : fallbackBot;
      
      myChatInfo = botId => chatInfo(myBot(botId));
    }

    private string this[int pollId] => $"poll:data:{pollId}";
    private string this[User user] => $"user:data:{user.Id}";

    private User GetCachedUser(long userId)
    {
      var user = new User { Id = userId };

      return myMemoryCache.Get<User>(this[user]) ?? user;
    }
    
    [CanBeNull] public Poll GetTemporaryPoll(int pollId) => myMemoryCache.Get<Poll>(this[pollId]);

    public async Task<int> GetPollId(Poll poll, User user, CancellationToken cancellationToken = default)
    {
      var nextId = poll.Id = await myContext.GetNextPollId(cancellationToken);

      using var pollEntry = myMemoryCache.CreateEntry(this[nextId]);
      pollEntry.Value = poll;
      pollEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3);

      using var userEntry = myMemoryCache.CreateEntry(this[user]);
      userEntry.Value = user;
      userEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3);

      return nextId;
    }

    public async Task<PollMessage> GetOrCreatePollAndMessage(PollMessage pollMessage, IUrlHelper urlHelper, VoteEnum? format = null, CancellationToken cancellationToken = default)
    {
      bool exRaidGym = false;
      var pollId = pollMessage.PollId;
      if (pollId < 0)
      {
        pollId = pollMessage.PollId = -pollId;
        exRaidGym = true;
      }
      var poll = pollMessage.Poll ?? await myContext
        .Set<Poll>()
        .Where(_ => _.Id == pollId)
        .IncludeRelatedData()
        .Include(poll => poll.Notifications)
        .FirstOrDefaultAsync(cancellationToken);

      if (poll != null)
      {
        var existingMessage = poll.Messages.FirstOrDefault(_ => _.InlineMessageId == pollMessage.InlineMessageId && _.ChatId == pollMessage.ChatId && _.MessageId == pollMessage.MessageId);
        if (existingMessage != null)
          return existingMessage;

        pollMessage.Poll = poll;
        return await AddPollMessage(pollMessage, urlHelper, cancellationToken);
      }

      var pollData = GetTemporaryPoll(pollId);
      if (pollData == null) return null;

      pollMessage.Poll = new Poll
      {
        Id = pollId,
        Title = pollData.Title,
        AllowedVotes = format,
        Owner = pollData.Owner,
        Portal = pollData.Portal,
        ExRaidGym = exRaidGym,
        Time = pollData.Time,
        TimeZoneId = pollData.TimeZoneId,
        Votes = pollData.Votes,
        Limits = pollData.Limits
      };
      if (pollMessage.UserId is { } userId)
        pollMessage.Poll.InitImplicitVotes(GetCachedUser(userId), pollMessage.BotId);
      
      // MUST be set before portal
      myContext.Set<Poll>().Attach(pollMessage.Poll).State = EntityState.Added;

      if (pollData.Portal is { } portal)
      {
        var portalSet = myContext.Set<Portal>();
        // always check remote
        var existingPortal = await portalSet.AsNoTracking().FirstOrDefaultAsync(p =>  p.Guid == portal.Guid, cancellationToken);
        if (existingPortal == null)
        {
          portalSet.Attach(portal).State = EntityState.Added;
        }
        else
        {
          myContext.Entry(existingPortal).SetNotNullProperties(portal);
          myContext.Entry(portal).SetNotNullProperties(existingPortal);
        }
      }
      
      return await AddPollMessage(pollMessage, urlHelper, cancellationToken, withLog: true);
    }

    public async Task<PollMessage> AddPollMessage([CanBeNull] PollMessage message, IUrlHelper urlHelper, CancellationToken cancellationToken = default, bool withLog = false)
    {
      if (message?.Poll == null)
        return message;
      
      message.Poll.AllowedVotes ??= await myContext.Set<Settings>().GetFormat(message.UserId, cancellationToken);

      var raidUpdated = false;
      var eggRaidUpdated = false;
      if (message.Poll?.Raid is { Id: 0 } raid)
      {
        var raidCopy = raid;
        var sameRaids = myContext
          .Set<Raid>()
          .Where(_ => _.Lon == raidCopy.Lon && _.Lat == raidCopy.Lat);
        var existingRaid = await sameRaids
          .Where(_ => _.RaidBossLevel == raidCopy.RaidBossLevel && _.Pokemon == raidCopy.Pokemon && _.EndTime == raidCopy.EndTime)
          .Include(_ => _.EggRaid)
          .IncludeRelatedData()
          .FirstOrDefaultAsync(cancellationToken);
        if (existingRaid != null)
        {
          if ((existingRaid.Gym ?? existingRaid.PossibleGym) == null && (raidCopy.Gym ?? raidCopy.PossibleGym) != null)
          {
            existingRaid.PossibleGym = raidCopy.Gym ?? raidCopy.PossibleGym;
            raidUpdated = true;
          }

          if (string.IsNullOrEmpty(message.Poll.Title))
          {
            // use existing poll if have rights for any prev message
            foreach (var existingRaidPoll in existingRaid.Polls)
            {
              foreach (var existingRaidPollMessage in existingRaidPoll.Messages)
              {
                if (await myChatInfo(existingRaidPollMessage.BotId).CanReadPoll(existingRaidPollMessage.ChatId ?? existingRaidPollMessage.Poll.Owner, message.UserId ?? message.ChatId, cancellationToken))
                {
                  if ((existingRaidPoll.Votes?.Count ?? 0) >= (message.Poll.Messages?.Count ?? 0))
                  {
                    message.Poll = existingRaidPoll;
                    break;
                  }
                }
              }

              if (message.Poll == existingRaidPoll)
                break;
            }
          }

          raid = message.Poll.Raid = existingRaid;
        }

        if (raid.Pokemon != null && raid.EggRaid == null) // check for egg raid
        {
          var eggRaid = await sameRaids
            .Where(_ => _.Pokemon == null && _.RaidBossEndTime == raid.RaidBossEndTime)
            .IncludeRelatedData()
            .DecompileAsync()
            .FirstOrDefaultAsync(cancellationToken);
          if (eggRaid != null && raid.Id != eggRaid.Id)
          {
            var eggRaidPolls = eggRaid.Polls ??= new List<Poll>(0);
            var raidPolls = raid.Polls ??= new List<Poll>(eggRaidPolls.Count);
            // on post egg raid creation update all existing polls to new raid
            foreach (var eggRaidPoll in new List<Poll>(eggRaidPolls))
            {
              eggRaidPolls.Remove(eggRaidPoll);
              raidPolls.Add(eggRaidPoll);
              eggRaidPoll.Raid = raid;
              raidUpdated = true;
                
              if (!string.IsNullOrEmpty(message.Poll.Title))
                continue;
                
              // use existing poll if have rights for any prev message
              foreach (var eggRaidPollMessage in eggRaidPoll.Messages)
              {
                if (await myChatInfo(eggRaidPollMessage.BotId).CanReadPoll(eggRaidPollMessage.ChatId ?? eggRaidPollMessage.Poll.Owner, message.UserId ?? message.ChatId, cancellationToken))
                {
                  if ((eggRaidPoll.Votes?.Count ?? 0) >= (message.Poll.Messages?.Count ?? 0))
                  {
                    message.Poll = eggRaidPoll;
                    break;
                  }
                }
              }
            }
            message.Poll.Raid = raid;
            raid.EggRaid = eggRaid;
            eggRaidUpdated = true;
          }
        }
      }

      myContext.Set<PollMessage>().Attach(message);
      await myContext.SaveChangesAsync(cancellationToken);

      // update current raid poll messages if changed
      if (raidUpdated && message.Poll.Raid is { } raidToUpdate)
      {
        foreach (var poll in raidToUpdate.Polls ?? Enumerable.Empty<Poll>())
        {
          await UpdatePoll(poll, urlHelper, cancellationToken);
        }
      }

      // update egg raid poll messages if any
      if (eggRaidUpdated && message.Poll.Raid?.EggRaid is { } eggRaidToUpdate)
      {
        foreach (var poll in eggRaidToUpdate.Polls ?? Enumerable.Empty<Poll>())
        {
          await UpdatePoll(poll, urlHelper, cancellationToken);
        }
      }

      var content = message.Poll.GetMessageText(urlHelper, message.Poll.DisableWebPreview());
      if (message.Chat is { } chat)
      {
        var bot = myBot(message.BotId);
        var postedMessage = await bot.SendTextMessageAsync(chat, content, cancellationToken: cancellationToken,
          replyMarkup: await message.GetReplyMarkup(myChatInfo(message.BotId), cancellationToken), disableNotification: true);
        message.BotId = bot.BotId;
        message.Chat = postedMessage.Chat;
        message.MessageId = postedMessage.MessageId;
      }
      else if (message.InlineMessageId is { })
      {
        // TODO: ???
      }

      await myContext.SaveChangesAsync(cancellationToken);

      // log message
      if (withLog && myLogChat != null)
      {
        await AddPollMessage(new PollMessage { BotId = message.BotId, ChatId = myLogChat, Poll = message.Poll } , urlHelper, cancellationToken);
      }

      return message;
    }

    public async Task UpdatePoll(Poll poll, IUrlHelper urlHelper, CancellationToken cancellationToken = default)
    {
      foreach (var message in poll.Messages)
      {
        await UpdatePollMessage(message, urlHelper, cancellationToken);
      }
    }

    public async Task UpdatePollMessage(PollMessage pollMessage, IUrlHelper urlHelper, CancellationToken cancellationToken = default)
    {
      Func<User, TextBuilder, TextBuilder> userFormatter = null;
      Func<IGrouping<VoteEnum?, Vote>, bool, TextBuilder, TextBuilder> userGroupFormatter = null;

      Func<IGrouping<VoteEnum?, Vote>, bool, TextBuilder, TextBuilder> UserGroupFormatter(string delimiter, Func<TextBuilder, TextBuilder> postAction = null) =>
        (vote, _, builder) =>
          builder
            .Sanitize(vote.Key?.Description())
            .Sanitize("\x00A0")
            .Code(b =>
            {
              var initialLength = b.Length;
              b = vote.OrderBy(v => v.Modified).Aggregate(b, (bb, v) =>
                  (userFormatter ?? UserEx.DefaultUserExtractor)(v.User, bb.Sanitize(bb.Length == initialLength ? null : delimiter)));
              postAction?.Invoke(b);
            })
            .NewLine();

      switch (pollMessage.PollMode)
      {
        case { } mode when mode.HasFlag(PollMode.Nicknames):
          userFormatter = await GetNicknamesUserFormatter(pollMessage.Poll, cancellationToken);
          userGroupFormatter = UserGroupFormatter(", ");
          goto default;
          
        case { } mode when mode.HasFlag(PollMode.Usernames):
          userFormatter = (user, b) => string.IsNullOrEmpty(user.Username) ? UserEx.DefaultUserExtractor(user, b) : b.Append($"@{user.Username}");
          userGroupFormatter = UserGroupFormatter(" ", b => b.Sanitize(" "));
          goto default;
        
        case { } mode when mode.HasFlag(PollMode.Invitation) || (pollMessage.Poll.AllowedVotes ?? VoteEnum.Standard).HasFlag(VoteEnum.Invitation):
          var nicknames = await GetNicknames(pollMessage.Poll, cancellationToken);
          userFormatter = (user, b) => nicknames.ContainsKey(user.Id) ?
            UserEx.DefaultUserExtractor(user, b) :
            b.Italic(bb => UserEx.DefaultUserExtractor(user, bb));
          goto default;
          
        default:
          await UpdatePollMessage(pollMessage, urlHelper, userFormatter, userGroupFormatter, cancellationToken);
          break;
      }
    }

    private async Task<Dictionary<long, string>> GetNicknames(Poll poll, CancellationToken cancellationToken = default)
    {
      return await myMemoryCache.GetOrCreateAsync($"Nicknames:{poll.Id}",
        async entry =>
        {
          entry.SlidingExpiration = TimeSpan.FromSeconds(3);

          var userIDs = poll.Votes.Select(_ => _.UserId).ToList();
          return (await myContext
              .Set<Player>()
              .Where(player => userIDs.Contains(player.UserId))
              .ToListAsync(cancellationToken))
            .Where(player => !string.IsNullOrEmpty(player.Nickname)) // filter out empty nicknames
            .ToDictionary(player => player.UserId, player => player.Nickname);
        });
    }

    private async Task<Func<User, TextBuilder, TextBuilder>> GetNicknamesUserFormatter(Poll poll, CancellationToken cancellationToken = default)
    {
      var nicknames = await GetNicknames(poll, cancellationToken);

      return (user, builder) => 
        nicknames.TryGetValue(user.Id, out var nickname) ? builder.SanitizeNickname(nickname) : 
          builder.Italic(b => _ = user.Username is {} username ? b.SanitizeNickname(username) : UserEx.NicknameSanitizingDefaultUserExtractor(user, b));
    }
    
    private async Task UpdatePollMessage(PollMessage message, IUrlHelper urlHelper, Func<User, TextBuilder, TextBuilder> userFormatter = null,
      Func<IGrouping<VoteEnum?, Vote>, bool, TextBuilder, TextBuilder> userGroupFormatter = null, CancellationToken cancellationToken = default)
    {
      try
      {
        var bot = myBot(message.BotId);
        var chatInfo = myChatInfo(message.BotId);
        var content = message.Poll.GetMessageText(urlHelper, userFormatter: userFormatter, userGroupFormatter: userGroupFormatter, disableWebPreview: message.Poll.DisableWebPreview());
        if (message.InlineMessageId is { } inlineMessageId)
        {
          await bot.EditMessageTextAsync(inlineMessageId, content, await message.GetReplyMarkup(chatInfo, cancellationToken), cancellationToken);
        }
        else if (message.ChatId is { } chatId && message.MessageId is { } messageId)
        {
          await bot.EditMessageTextAsync(chatId, messageId, content, await message.GetReplyMarkup(chatInfo, cancellationToken), cancellationToken);
        }
      }
      catch (Exception ex)
      {
        myTelemetryClient.TrackExceptionEx(ex, message.GetTrackingProperties(new Dictionary<string, string>{ { nameof(ITelegramBotClient.BotId), message.BotId?.ToString() } }));
      }
    }
  }
}