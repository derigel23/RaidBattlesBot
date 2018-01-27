using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Model
{
  public static class PollMessageEx
  {
    public static async Task<InlineKeyboardMarkup> GetReplyMarkup(this PollMessage message, ChatInfo chatInfo, CancellationToken cancellationToken = default)
    {
      var pollReplyMarkup = message.Poll.GetReplyMarkup();
      var inlineKeyboardButtons = pollReplyMarkup?.InlineKeyboard;

      var messageChat = message.Chat;
      if (messageChat == null) // inline message
        return pollReplyMarkup;

      switch (messageChat.Type)
      {
        case ChatType.Channel:
          // channel, cancelled
          if (pollReplyMarkup == null)
            return null;
            
          // channel, not cancelled
          // replace share button with clone button
          for (var i = 0; i < inlineKeyboardButtons.Length; i++)
          for (var j = 0; j < inlineKeyboardButtons[i].Length; j++)
          {
            if (inlineKeyboardButtons[i][j].Text == "🌐" )
              inlineKeyboardButtons[i][j] = new InlineKeyboardCallbackButton("🌐", $"clone:{message.GetPollId()}");
          }

          return pollReplyMarkup;
        
        case ChatType.Group:
        case ChatType.Supergroup:
          return pollReplyMarkup;
      }

      if (!await chatInfo.IsAdmin(message.Poll.Owner, message.UserId, cancellationToken))
        return pollReplyMarkup;

      if (pollReplyMarkup == null) // cancelled
      {
        if (!message.Poll.Cancelled)
          return null; // can't be

        return  new InlineKeyboardMarkup(new InlineKeyboardButton[]
        {
          new InlineKeyboardCallbackButton("Возобновить", $"restore:{message.GetPollId()}"),
        });
      }

      var length = inlineKeyboardButtons.GetLength(0);
      var pollMessageReplyMarkup = new InlineKeyboardButton[length + 1][];
      Array.Copy(inlineKeyboardButtons, pollMessageReplyMarkup, length);
      var additionalKeyboardButtons = new List<InlineKeyboardButton>();
      if (message.Poll.Time != null)
      {
        additionalKeyboardButtons.Add(new InlineKeyboardCallbackButton("5' раньше", $"adjust:{message.GetPollId()}:-5"));
        additionalKeyboardButtons.Add(new InlineKeyboardCallbackButton("5' позже", $"adjust:{message.GetPollId()}:5"));
      }
      additionalKeyboardButtons.Add(new InlineKeyboardCallbackButton("Отменить", $"cancel:{message.GetPollId()}"));
      pollMessageReplyMarkup[length] = additionalKeyboardButtons.ToArray();
      return new InlineKeyboardMarkup(pollMessageReplyMarkup);
    }

    public static int? GetPollId(this PollMessage message)
    {
      return message.Poll?.Id ?? message.PollId;
    }
    
    public static IQueryable<PollMessage> IncludeRelatedData(this IQueryable<PollMessage> pollMessages)
    {
      return pollMessages
        .Include(_ => _.Poll)
        .ThenInclude(_ => _.Votes)
        .Include(_ => _.Poll)
        .ThenInclude(_ => _.Messages)
        .Include(_ => _.Poll)
        .ThenInclude(_ => _.Raid)
        .ThenInclude(raid => raid.PostEggRaid);
    }
  }
} 