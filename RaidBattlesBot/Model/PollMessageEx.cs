using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnumsNET;
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
      var pollReplyMarkup = message.Poll.GetReplyMarkup(message.PollMode);
      
      var messageChat = message.Chat;
      if (messageChat == null) // inline message
        return pollReplyMarkup;

      if (pollReplyMarkup == null) // cancelled poll
        return null;

      var inlineKeyboardButtons = pollReplyMarkup.InlineKeyboard.Select(_ => _.ToArray()).ToArray();

      bool modified = false;
      for (var i = 0; i < inlineKeyboardButtons.Length; i++)
      for (var j = 0; j < inlineKeyboardButtons[i].Length; j++)
      {
        var inlineKeyboardButton = inlineKeyboardButtons[i][j];

        // channel, not cancelled
        // replace share button with clone button
        if (messageChat.Type == ChatType.Channel && (inlineKeyboardButton.CallbackData?.StartsWith(ShareInlineQueryHandler.ID) ?? false))
        {
          modified = true;
          inlineKeyboardButtons[i][j] = InlineKeyboardButton.WithCallbackData(VoteEnum.Share.AsString(EnumFormat.DisplayName), $"{CloneCallbackQueryHandler.ID}:{message.GetExtendedPollId()}");
        }
        
        // For Invite button on direct messages - direct reply in the same chat
        if ((inlineKeyboardButton.SwitchInlineQuery?.StartsWith(InviteInlineQueryHandler.PREFIX) ?? false) ||
            (inlineKeyboardButton.SwitchInlineQueryCurrentChat?.StartsWith(InviteInlineQueryHandler.PREFIX) ?? false))
        {
          modified = true;
          inlineKeyboardButtons[i][j] = InlineKeyboardButton.WithCallbackData(inlineKeyboardButton.Text, $"{InviteCallbackQueryHandler.ID}:{message.GetExtendedPollId()}");
        }
      }

      if (modified)
      {
        pollReplyMarkup = new InlineKeyboardMarkup(inlineKeyboardButtons);
      }
      
      if (messageChat.Type != ChatType.Private) return pollReplyMarkup;

      // TODO: Currently, no additional admin buttons
      return pollReplyMarkup;
      
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

    private static readonly IDictionary<string, string> EmptyProperties = new Dictionary<string, string>(0);
    
    public static IDictionary<string, string> GetTrackingProperties([CanBeNull] this PollMessage pollMessage, IDictionary<string, string> properties = null)
    {
      return new Dictionary<string, string>(properties ?? EmptyProperties)
      {
        { "messageId", pollMessage?.Id is { } pollMessageId && pollMessageId > 0 ? pollMessageId.ToString() : null },
        { "pollId", pollMessage?.GetPollId()?.ToString() },
        { "raidId", pollMessage?.Poll.GetRaidId()?.ToString() }
      };
    }
  }
} 