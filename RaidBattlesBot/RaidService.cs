using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelegateDecompiler.EntityFramework;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace RaidBattlesBot
{
  public class RaidService
  {
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;
    private readonly TelemetryClient myTelemetryClient;
    private readonly ChatInfo myChatInfo;
    private readonly UserInfo myUserInfo;
    private readonly IMemoryCache myMemoryCache;

    public RaidService(RaidBattlesContext context, ITelegramBotClient bot, TelemetryClient telemetryClient, ChatInfo chatInfo, UserInfo userInfo, IMemoryCache memoryCache)
    {
      myContext = context;
      myBot = bot;
      myTelemetryClient = telemetryClient;
      myChatInfo = chatInfo;
      myUserInfo = userInfo;
      myMemoryCache = memoryCache;
    }

    public async Task<int> GetPollId(InlineQuery data, CancellationToken cancellationToken = default)
    {
      using (var connection = myContext.Database.GetDbConnection())
      {
        try
        {
          await connection.OpenAsync(cancellationToken);
          using (var command = connection.CreateCommand())
          {
            command.CommandText = "SELECT NEXT VALUE FOR PollId";
            var pollId = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            using (var entry = myMemoryCache.CreateEntry(this[pollId]))
            {
              entry.Value = data;
              entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3);
            }
            return pollId;
          }
        }
        finally
        {
          connection.Close();
        }
      }

    }

    public async Task<PollMessage> GetOrCreatePollAndMessage(PollMessage pollMessage, IUrlHelper urlHelper, CancellationToken cancellationToken = default)
    {
      var pollId = pollMessage.PollId;
      var poll = await myContext
        .Set<Poll>()
        .Where(_ => _.Id == pollId)
        .IncludeRelatedData()
        .FirstOrDefaultAsync(cancellationToken);

      if (poll != null)
      {
        var existingMessage = poll.Messages.SingleOrDefault(_ => _.InlineMesssageId == pollMessage.InlineMesssageId && _.ChatId == pollMessage.ChatId && _.MesssageId == pollMessage.MesssageId);
        if (existingMessage != null)
          return existingMessage;

        pollMessage.Poll = poll;
        return await AddPollMessage(pollMessage, urlHelper, cancellationToken);
      }

      var votesFormatIndex = (pollId - RaidBattlesContext.PollIdSeed) % VoteEnumEx.AllowedVoteFormats.Length;
      var pollIdBase = pollId - votesFormatIndex;
      var pollData = myMemoryCache.Get<InlineQuery>(this[pollIdBase]);
      if (pollData == null) return null;

      pollMessage.Poll = new Poll
      {
        Id = pollId,
        Title = pollData.Query,
        AllowedVotes = VoteEnumEx.AllowedVoteFormats[votesFormatIndex],
        Owner = pollData.From.Id,
        Votes = new List<Vote>()
      };
      var pollEntity = myContext.Attach(pollMessage.Poll);
      pollEntity.State = EntityState.Added;

      return await AddPollMessage(pollMessage, urlHelper, cancellationToken);
    }

    private string this[int pollId] => $"poll:data:{pollId}";

    public async Task<PollMessage> AddPollMessage([CanBeNull] PollMessage message, IUrlHelper urlHelper, CancellationToken cancellationToken = default)
    {
      if (message?.Poll == null)
        return message;
      
      message.Poll.AllowedVotes =
        message.Poll.AllowedVotes ?? (await myContext.Set<Settings>().FirstOrDefaultAsync(settings => settings.Chat == message.ChatId, cancellationToken))?.DefaultAllowedVotes;

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

      var messageEntity = myContext.Attach(message);
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

      var messageText = (await message.Poll.GetMessageText(urlHelper, myUserInfo, RaidEx.ParseMode, cancellationToken)).ToString();
      if (message.Chat is Chat chat)
      {
        var postedMessage = await myBot.SendTextMessageAsync(chat, messageText, RaidEx.ParseMode, disableWebPagePreview: message.Poll.GetRaidId() == null,
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

      return message;
    }

    public async Task UpdatePoll(Poll poll, IUrlHelper urlHelper, CancellationToken cancellationToken = default)
    {
      var messageText = (await poll.GetMessageText(urlHelper, myUserInfo, RaidEx.ParseMode, cancellationToken)).ToString();
      foreach (var message in poll.Messages)
      {
        try
        {
          if (message.InlineMesssageId is string inlineMessageId)
          {
            await myBot.EditMessageTextAsync(inlineMessageId, messageText, RaidEx.ParseMode, disableWebPagePreview: poll.GetRaidId() == null,
              replyMarkup: await message.GetReplyMarkup(myChatInfo, cancellationToken), cancellationToken: cancellationToken);
          }
          else if (message.ChatId is long chatId && message.MesssageId is int messageId)
          {
            await myBot.EditMessageTextAsync(chatId, messageId, messageText, RaidEx.ParseMode, disableWebPagePreview: poll.GetRaidId() == null,
              replyMarkup: await message.GetReplyMarkup(myChatInfo, cancellationToken), cancellationToken: cancellationToken);
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