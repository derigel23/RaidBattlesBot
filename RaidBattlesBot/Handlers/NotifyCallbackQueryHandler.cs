﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Team23.TelegramSkeleton;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = ID)]
  public class NotifyCallbackQueryHandler : ICallbackQueryHandler
  {
    public const string ID = "notify";

    private readonly RaidService myRaidService;
    private readonly IUrlHelper myUrlHelper;
    private readonly ITelegramBotClient myBot;
    private readonly TelemetryClient myTelemetryClient;

    public NotifyCallbackQueryHandler(RaidService raidService, IUrlHelper urlHelper, ITelegramBotClient bot, TelemetryClient telemetryClient)
    {
      myRaidService = raidService;
      myUrlHelper = urlHelper;
      myBot = bot;
      myTelemetryClient = telemetryClient;
    }

    private const int NotificationBatchSize = 5;
    
    public async Task<(string, bool, string)> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != ID)
        return (null, false, null);

      PollMessage pollMessage;
      if (callback.ElementAtOrDefault(1) is var pollIdSegment && PollEx.TryGetPollId(pollIdSegment, out var pollId, out var format))
      {
        pollMessage = await myRaidService.GetOrCreatePollAndMessage(new PollMessage(data) { BotId = myBot.BotId, PollId = pollId }, myUrlHelper, format, cancellationToken);
      }
      else
        return ("Poll is publishing. Try later.", true, null);

      if (pollMessage?.Poll is var poll && poll == null)
        return ("Poll is not found", true, null);

      if (!poll.AllowedVotes?.HasFlag(VoteEnum.Invitation) ?? true)
        return (null, false, null);

      var inviteVotes = poll.Votes
        .Where(vote => vote.Team?.HasFlag(VoteEnum.Invitation) ?? false)
        .OrderBy(vote => vote.Modified)
        .ToList();
      var invitePartitionedVotes = inviteVotes.Chunk(NotificationBatchSize);
      
      var inviteMessages = invitePartitionedVotes.Select(votes =>
        votes.Aggregate(new TextBuilder(),
          (builder, vote) => string.IsNullOrEmpty(vote.Username) ? vote.User.GetLink(builder) : builder.Append($"@{vote.Username}, "),
          builder =>
          {
            builder.Length -= 2;
            return builder.ToTextMessageContent();
          })
      ).ToArray();
      
      if (inviteMessages.Length == 0)
        return ("Nobody to notify", false, null);

      var targetChat = data.Message?.Chat ?? new Chat { Id = data.From.Id };
      
      foreach (var message in inviteMessages)
      {
        try
        {
          await myBot.SendTextMessageAsync(targetChat, message, disableNotification: true, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
          myTelemetryClient.TrackExceptionEx(ex, pollMessage.GetTrackingProperties(new Dictionary<string, string>
          {
            { nameof(ITelegramBotClient.BotId), myBot.BotId.ToString()}
          }));
        }
      }

      return ("You must copy a notification message and post it by yourself", true, null);
    }
  }
}