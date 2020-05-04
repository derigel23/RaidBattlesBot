using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using RaidBattlesBot.Handlers;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Model
{
  public static class PollMessageEx
  {
    public static async Task<InlineKeyboardMarkup> GetReplyMarkup(this PollMessage message, ChatInfo chatInfo, CancellationToken cancellationToken = default)
    {
      var pollReplyMarkup = message.Poll.GetReplyMarkup();
      
      var messageChat = message.Chat;
      if (messageChat == null) // inline message
        return pollReplyMarkup;

      switch (messageChat.Type)
      {
        case ChatType.Channel:
          // channel, cancelled
          if (pollReplyMarkup == null)
            return null;

          var inlineKeyboardButtons = pollReplyMarkup.InlineKeyboard.Select(_ => _.ToArray()).ToArray();

          // channel, not cancelled
          // replace share button with clone button
          for (var i = 0; i < inlineKeyboardButtons.Length; i++)
          for (var j = 0; j < inlineKeyboardButtons[i].Length; j++)
          {
            if (inlineKeyboardButtons[i][j].Text == "🌐" )
              inlineKeyboardButtons[i][j] = InlineKeyboardButton.WithCallbackData("🌐", $"clone:{message.GetExtendedPollId()}");
          }

          return new InlineKeyboardMarkup(inlineKeyboardButtons);
        
        case ChatType.Group:
        case ChatType.Supergroup:
          return pollReplyMarkup;
      }

      if (!await chatInfo.CandEditPoll(message.Poll.Owner, message.UserId, cancellationToken))
        return pollReplyMarkup;

      var pollId = message.GetExtendedPollId();
      if (pollReplyMarkup == null) // cancelled
      {
        if (!message.Poll.Cancelled)
          return null; // can't be

        return  new InlineKeyboardMarkup(new []
        {
          InlineKeyboardButton.WithCallbackData("Resume", $"{RestoreCallbackQueryHandler.ID}:{pollId}"),
        });
      }

      var additionalKeyboardButtons = new List<InlineKeyboardButton>();
      if (message.Poll.Time != null)
      {
        additionalKeyboardButtons.Add(InlineKeyboardButton.WithCallbackData("5' earlier", $"{AdjustCallbackQueryHandler.ID}:{pollId}:-5"));
        additionalKeyboardButtons.Add(InlineKeyboardButton.WithCallbackData("5' later", $"{AdjustCallbackQueryHandler.ID}:{pollId}:5"));
      }
      additionalKeyboardButtons.Add(InlineKeyboardButton.WithCallbackData("Cancel", $"{CancelCallbackQueryHandler.ID}:{pollId}"));
      return new InlineKeyboardMarkup(pollReplyMarkup.InlineKeyboard.Append(additionalKeyboardButtons));
    }

    public static int? GetPollId(this PollMessage message)
    {
      return message.Poll?.Id ?? message.PollId;
    }

    public static PollId GetExtendedPollId(this PollMessage message)
    {
      return message.Poll?.GetId() ?? new PollId { Id = message.PollId };
    }
    
    public static IQueryable<PollMessage> IncludeRelatedData(this IQueryable<PollMessage> pollMessages)
    {
      return pollMessages
        .Include(_ => _.Poll)
        .ThenInclude(_ => _.Votes)
        .Include(_ => _.Poll)
        .ThenInclude(_ => _.Messages)
        .Include(_ => _.Poll)
        .ThenInclude(_ => _.Portal)
        .Include(_ => _.Poll)
        .ThenInclude(_ => _.Raid)
        .ThenInclude(raid => raid.PostEggRaid);
    }

    public static IDictionary<string, string> GetTrackingProperties([CanBeNull] this PollMessage pollMessage)
    {
      return new Dictionary<string, string>
      {
        { "messageId", pollMessage?.Id is int pollMessageId && pollMessageId > 0 ? pollMessageId.ToString() : null },
        { "pollId", pollMessage?.GetPollId()?.ToString() },
        { "raidId", pollMessage?.Poll.GetRaidId()?.ToString() }
      };
    }
  }
} 