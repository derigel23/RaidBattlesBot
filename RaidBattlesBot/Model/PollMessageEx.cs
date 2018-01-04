using System;
using System.Collections.Generic;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Model
{
  public static class PollMessageEx
  {
    public static InlineKeyboardMarkup GetReplyMarkup(this PollMessage message)
    {
      var pollReplyMarkup = message.Poll.GetReplyMarkup();

      if ((message.ChatId == null) || (message.Poll.Owner != message.ChatId))
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

      var inlineKeyboardButtons = pollReplyMarkup.InlineKeyboard;
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
  }
} 