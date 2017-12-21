using System;
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
      pollMessageReplyMarkup[length] = new InlineKeyboardButton[]
      {
        new InlineKeyboardCallbackButton("Отменить", $"cancel:{message.GetPollId()}"),
      };
      return new InlineKeyboardMarkup(pollMessageReplyMarkup);
    }

    public static int? GetPollId(this PollMessage message)
    {
      return message.Poll?.Id ?? message.PollId;
    }
  }
} 