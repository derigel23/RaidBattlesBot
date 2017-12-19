using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Handlers;
using RaidBattlesBot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [CallbackQueryHandler(DataPrefix = "vote")]
  public class VoteCallbackQueryHandler : ICallbackQueryHandler
  {
    private readonly TelemetryClient myTelemetryClient;
    private readonly IHttpContextAccessor myHttpContextAccessor;
    private readonly RaidBattlesContext myContext;
    private readonly ITelegramBotClient myBot;

    public VoteCallbackQueryHandler(TelemetryClient telemetryClient, IHttpContextAccessor httpContextAccessor, RaidBattlesContext context, ITelegramBotClient bot)
    {
      myTelemetryClient = telemetryClient;
      myHttpContextAccessor = httpContextAccessor;
      myContext = context;
      myBot = bot;
    }

    public async Task<string> Handle(CallbackQuery data, object context = default, CancellationToken cancellationToken = default)
    {
      var callback = data.Data.Split(':');
      if (callback[0] != "vote")
        return null;
      
      if (!int.TryParse(callback.ElementAt(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var pollId))
        return null;

      var poll = await myContext
        .Polls
        .Where(_ => _.Id == pollId)
        .Include(_ => _.Votes)
        .Include(_ => _.Messages)
        .Include(_ => _.Raid)
        .FirstOrDefaultAsync(cancellationToken);

      if (poll == null)
        return null;

      var user = data.From;

      var vote = poll.Votes.SingleOrDefault(v => v.UserId == user.Id);
      if (vote == null)
      {
        poll.Votes.Add(vote = new Vote());
      }

      vote.User = user; // update firstname/lastname if necessary

      int? GetTeam(string team)
      {
        switch (team)
        {
          case "red": return 0;
          case "yellow": return 1;
          case "blue": return 2;
          case "none": return 3;
          case "cancel": return 4;
        }

        return null;
      }

      var teamStr = callback.ElementAt(2);
      vote.Team = GetTeam(teamStr);
      var changed = await myContext.SaveChangesAsync(cancellationToken) > 0;
      if (changed)
      {

        var messageText = poll.GetMessageText().ToString();
        foreach (var message in poll.Messages)
        {
          try
          {
            if (message.InlineMesssageId != null)
            {
              await myBot.EditInlineMessageTextAsync(message.InlineMesssageId, messageText, ParseMode.Markdown,
                replyMarkup: poll.GetReplyMarkup(), cancellationToken: cancellationToken);
            }
            else
            {
              await myBot.EditMessageTextAsync(message.Chat, message.MesssageId.GetValueOrDefault(), messageText, ParseMode.Markdown,
                replyMarkup: poll.GetReplyMarkup(), cancellationToken: cancellationToken);
            }
          }
          catch (Exception ex)
          {
            myTelemetryClient.TrackException(ex, myHttpContextAccessor.HttpContext.Properties());
          }
        }
      }

      return changed ? $"You've voted for {teamStr}" : "You've already voted.";
    }
  }
}