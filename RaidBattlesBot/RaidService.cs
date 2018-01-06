﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelegateDecompiler.EntityFramework;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot
{
  public class RaidService
  {
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;
    private readonly TelemetryClient myTelemetryClient;
    private readonly IHttpContextAccessor myHttpContextAccessor;

    public RaidService(RaidBattlesContext context, ITelegramBotClient bot, TelemetryClient telemetryClient, IHttpContextAccessor httpContextAccessor)
    {
      myContext = context;
      myBot = bot;
      myTelemetryClient = telemetryClient;
      myHttpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> AddPoll(string text, PollMessage message, IUrlHelper urlHelper, CancellationToken cancellationToken = default)
    {
      message.Poll = new Poll
      {
        Title = text,
        Owner = message.UserId
      };

      return await AddPollMessage(message, urlHelper, cancellationToken);
    }


    public async Task<bool> AddPollMessage(PollMessage message, IUrlHelper urlHelper, CancellationToken cancellationToken = default)
    {
      var raidUpdated = false;
      var eggRaidUpdated = false;
      if (message.Poll.Raid is Raid raid)
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
              raid.EggRaid = eggRaid;
              eggRaidUpdated = true;
            }
          }
        }
      }

      var messageEntity = myContext.Attach(message);
      await myContext.SaveChangesAsync(cancellationToken);
      
      // update egg raid poll messages if any
      if (eggRaidUpdated && message.Poll?.Raid?.EggRaid is Raid eggRaidToUpdate)
      {
        foreach (var poll in eggRaidToUpdate.Polls ?? Enumerable.Empty<Poll>())
        {
          await UpdatePoll(poll, urlHelper, cancellationToken);
        }
      }

      // update current raid poll messages if changed
      if (raidUpdated && message.Poll?.Raid is Raid raidToUpdate)
      {
        foreach (var poll in raidToUpdate.Polls ?? Enumerable.Empty<Poll>())
        {
          if (poll == message.Poll) continue; // will be updated separately later
          await UpdatePoll(poll, urlHelper, cancellationToken);
        }
      }

      var messageText = message.Poll.GetMessageText(urlHelper);
      if (message.InlineMesssageId == null)
      {
        var postedMessage = await myBot.SendTextMessageAsync(message.Chat, messageText, ParseMode.Markdown,
          replyMarkup: message.GetReplyMarkup(), disableNotification: true, cancellationToken: cancellationToken);
        message.Chat = postedMessage.Chat;
        message.MesssageId = postedMessage.MessageId;
      }
      else
      {
        await myBot.EditInlineMessageTextAsync(message.InlineMesssageId, messageText, ParseMode.Markdown,
          replyMarkup: message.GetReplyMarkup(), cancellationToken: cancellationToken);
      }

      return await myContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task UpdatePoll(Poll poll, IUrlHelper urlHelper, CancellationToken cancellationToken = default)
    {
      var messageText = poll.GetMessageText(urlHelper);
      foreach (var message in poll.Messages)
      {
        try
        {
          if (message.InlineMesssageId != null)
          {
            await myBot.EditInlineMessageTextAsync(message.InlineMesssageId, messageText, ParseMode.Markdown,
              replyMarkup: message.GetReplyMarkup(), cancellationToken: cancellationToken);
          }
          else
          {
            await myBot.EditMessageTextAsync(message.Chat, message.MesssageId.GetValueOrDefault(), messageText, ParseMode.Markdown,
              replyMarkup: message.GetReplyMarkup(), cancellationToken: cancellationToken);
          }
        }
        catch (Exception ex)
        {
          myTelemetryClient.TrackException(ex, myHttpContextAccessor.HttpContext.Properties());
        }
      }
    }
  }
}