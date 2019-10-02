using System;
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

using Poll = RaidBattlesBot.Model.Poll;

namespace RaidBattlesBot
{
  [UsedImplicitly]
  public class RaidService
  {
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;
    private readonly TelemetryClient myTelemetryClient;
    private readonly ChatInfo myChatInfo;
    private readonly IMemoryCache myMemoryCache;
    private readonly long? myLogChat;

    public RaidService(RaidBattlesContext context, ITelegramBotClient bot, TelemetryClient telemetryClient, ChatInfo chatInfo, IMemoryCache memoryCache, IOptions<BotConfiguration> botOptions)
    {
      myContext = context;
      myBot = bot;
      myTelemetryClient = telemetryClient;
      myChatInfo = chatInfo;
      myMemoryCache = memoryCache;
      myLogChat = botOptions.Value?.LogChatId is long chatId && chatId != default ? chatId : default(long?);
    }
    
    private string this[int pollId] => $"poll:data:{pollId}";

    [CanBeNull] public Poll GetTemporaryPoll(int pollId) => myMemoryCache.Get<Poll>(this[pollId]);

    public async Task<int> GetPollId(Poll poll, CancellationToken cancellationToken = default)
    {
      var nextId = poll.Id = await myContext.GetNextPollId(cancellationToken);
      using (var entry = myMemoryCache.CreateEntry(this[nextId]))
      {
        entry.Value = poll;
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3);
      }
      
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
      var poll = await myContext
        .Set<Poll>()
        .Where(_ => _.Id == pollId)
        .IncludeRelatedData()
        .FirstOrDefaultAsync(cancellationToken);

      if (poll != null)
      {
        var existingMessage = poll.Messages.FirstOrDefault(_ => _.InlineMesssageId == pollMessage.InlineMesssageId && _.ChatId == pollMessage.ChatId && _.MesssageId == pollMessage.MesssageId);
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
        Votes = new List<Vote>()
      };
      myContext.Set<Poll>().Attach(pollMessage.Poll).State = EntityState.Added;
      if (pollData.Portal is Portal portal)
      {
        var portalSet = myContext.Set<Portal>();
        var existingPortal = await portalSet.AsTracking().FirstOrDefaultAsync(p => p.Guid == portal.Guid, cancellationToken);
        if (existingPortal == null)
        {
          portalSet.Attach(portal).State = EntityState.Added;
        }
        else
        {
          existingPortal.Guid = portal.Guid;
          existingPortal.Name = portal.Name;
          existingPortal.Address = portal.Address;
          existingPortal.Image = portal.Image;
          existingPortal.Latitude = portal.Latitude;
          existingPortal.Longitude = portal.Longitude;
          portalSet.Attach(portal).State = EntityState.Modified;
        }
      }
      
      return await AddPollMessage(pollMessage, urlHelper, cancellationToken, withLog: true);
    }

    public async Task<PollMessage> AddPollMessage([CanBeNull] PollMessage message, IUrlHelper urlHelper, CancellationToken cancellationToken = default, bool withLog = false)
    {
      if (message?.Poll == null)
        return message;
      
      message.Poll.AllowedVotes =
        message.Poll.AllowedVotes ?? await myContext.Set<Settings>().GetFormat(message.UserId, cancellationToken);

      var raidUpdated = false;
      var eggRaidUpdated = false;
      if (message.Poll?.Raid is Raid raid)
      {
        if (raid.Id == 0)
        {
          var sameRaids = myContext
            .Set<Raid>()
            .Where(_ => _.Lon == raid.Lon && _.Lat == raid.Lat);
          var existingRaid = await sameRaids
            .Where(_ => _.RaidBossLevel == raid.RaidBossLevel && _.Pokemon == raid.Pokemon && _.EndTime == raid.EndTime)
            .Include(_ => _.EggRaid)
            .IncludeRelatedData()
            .FirstOrDefaultAsync(cancellationToken);
          if (existingRaid != null)
          {
            if (((existingRaid.Gym ?? existingRaid.PossibleGym) == null) && ((raid.Gym ?? raid.PossibleGym) != null))
            {
              existingRaid.PossibleGym = raid.Gym ?? raid.PossibleGym;
              raidUpdated = true;
            }

            if (string.IsNullOrEmpty(message.Poll.Title))
            {
              // use existing poll if have rights for any prev message
              foreach (var existingRaidPoll in existingRaid.Polls)
              {
                foreach (var existingRaidPollMessage in existingRaidPoll.Messages)
                {
                  if (await myChatInfo.CanReadPoll(existingRaidPollMessage.ChatId ?? existingRaidPollMessage.Poll.Owner, message.UserId ?? message.ChatId, cancellationToken))
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

          if ((raid.Pokemon != null) && (raid.EggRaid == null)) // check for egg raid
          {
            var eggRaid = await sameRaids
              .Where(_ => _.Pokemon == null && _.RaidBossEndTime == raid.RaidBossEndTime)
              .IncludeRelatedData()
              .DecompileAsync()
              .FirstOrDefaultAsync(cancellationToken);
            if ((eggRaid != null) && (raid.Id != eggRaid.Id))
            {
              var eggRaidPolls = eggRaid.Polls = eggRaid.Polls ?? new List<Poll>(0);
              var raidPolls = raid.Polls = raid.Polls ?? new List<Poll>(eggRaidPolls.Count);
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
                  if (await myChatInfo.CanReadPoll(eggRaidPollMessage.ChatId ?? eggRaidPollMessage.Poll.Owner, message.UserId ?? message.ChatId, cancellationToken))
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
      }

      var messageEntity = myContext.Set<PollMessage>().Attach(message);
      await myContext.SaveChangesAsync(cancellationToken);

      // update current raid poll messages if changed
      if (raidUpdated && message.Poll.Raid is Raid raidToUpdate)
      {
        foreach (var poll in raidToUpdate.Polls ?? Enumerable.Empty<Poll>())
        {
          await UpdatePoll(poll, urlHelper, cancellationToken);
        }
      }

      // update egg raid poll messages if any
      if (eggRaidUpdated && message.Poll.Raid?.EggRaid is Raid eggRaidToUpdate)
      {
        foreach (var poll in eggRaidToUpdate.Polls ?? Enumerable.Empty<Poll>())
        {
          await UpdatePoll(poll, urlHelper, cancellationToken);
        }
      }

      var content = message.Poll.GetMessageText(urlHelper, disableWebPreview: message.Poll.DisableWebPreview());
      if (message.Chat is Chat chat)
      {
        var postedMessage = await myBot.SendTextMessageAsync(chat, content.MessageText, content.ParseMode, content.DisableWebPagePreview,
          replyMarkup: await message.GetReplyMarkup(myChatInfo, cancellationToken), disableNotification: true,
          
          cancellationToken: cancellationToken);
        message.Chat = postedMessage.Chat;
        message.MesssageId = postedMessage.MessageId;
      }
      else if (message.InlineMesssageId is string inlineMessageId)
      {
        //await myBot.EditInlineMessageTextAsync(inlineMessageId, messageText, RaidEx.ParseMode, disableWebPagePreview: message.Poll.GetRaidId() == null,
        //  replyMarkup: await message.GetReplyMarkup(myChatInfo, cancellationToken), cancellationToken: cancellationToken);
      }

      await myContext.SaveChangesAsync(cancellationToken);

      // log message
      if (withLog && myLogChat != null)
      {
        await AddPollMessage(new PollMessage { ChatId = myLogChat, Poll = message.Poll } , urlHelper, cancellationToken);
      }

      return message;
    }

    public async Task UpdatePoll(Poll poll, IUrlHelper urlHelper, CancellationToken cancellationToken = default)
    {
      var content = poll.GetMessageText(urlHelper, disableWebPreview: poll.DisableWebPreview());
      foreach (var message in poll.Messages)
      {
        try
        {
          if (message.InlineMesssageId is string inlineMessageId)
          {
            await myBot.EditMessageTextAsync(inlineMessageId, content.MessageText, content.ParseMode, content.DisableWebPagePreview,
              await message.GetReplyMarkup(myChatInfo, cancellationToken), cancellationToken);
          }
          else if (message.ChatId is long chatId && message.MesssageId is int messageId)
          {
            await myBot.EditMessageTextAsync(chatId, messageId, content.MessageText, content.ParseMode, content.DisableWebPagePreview,
              await message.GetReplyMarkup(myChatInfo, cancellationToken), cancellationToken);
          }
        }
        catch (Exception ex)
        {
          myTelemetryClient.TrackExceptionEx(ex, message.GetTrackingProperties());
        }
      }
    }
  }
}