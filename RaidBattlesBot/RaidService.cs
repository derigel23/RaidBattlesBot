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

    public RaidService(RaidBattlesContext context, ITelegramBotClient bot, TelemetryClient telemetryClient, ChatInfo chatInfo, UserInfo userInfo)
    {
      myContext = context;
      myBot = bot;
      myTelemetryClient = telemetryClient;
      myChatInfo = chatInfo;
      myUserInfo = userInfo;
    }

    public async Task<bool> AddPoll(string text, VoteEnum allowedVotes, PollMessage message, IUrlHelper urlHelper, CancellationToken cancellationToken = default)
    {
      message.Poll = new Poll
      {
        Title = text,
        AllowedVotes = allowedVotes,
        Owner = message.UserId,
      };

      return await AddPollMessage(message, urlHelper, cancellationToken);
    }

    public async Task<bool> AddPollMessage([CanBeNull] PollMessage message, IUrlHelper urlHelper, CancellationToken cancellationToken = default)
    {
      if (message?.Poll == null)
        return false;
      
      message.Poll.AllowedVotes =
        message.Poll.AllowedVotes ?? (await myContext.Settings.FirstOrDefaultAsync(settings => settings.Chat == message.ChatId, cancellationToken))?.DefaultAllowedVotes;

      var raidUpdated = false;
      var eggRaidUpdated = false;
      if (message.Poll?.Raid is Raid raid)
      {
        if (raid.Id == 0)
        {
          var sameRaids = myContext.Raids
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
              // use exisiting poll if have rights for any prev message
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
                
                // use exisiting poll if have rights for any prev message
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
        await myBot.EditInlineMessageTextAsync(inlineMessageId, messageText, RaidEx.ParseMode, disableWebPagePreview: message.Poll.GetRaidId() == null,
          replyMarkup: await message.GetReplyMarkup(myChatInfo, cancellationToken), cancellationToken: cancellationToken);
      }

      return await myContext.SaveChangesAsync(cancellationToken) > 0;
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
            await myBot.EditInlineMessageTextAsync(inlineMessageId, messageText, RaidEx.ParseMode, disableWebPagePreview: poll.GetRaidId() == null,
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
          myTelemetryClient.TrackException(ex);
        }
      }
    }
  }
}