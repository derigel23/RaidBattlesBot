using System;
using System.Runtime.InteropServices.ComTypes;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaidBattlesBot.Model
{
  public static class PollMessageEx
  {
    public static InlineKeyboardMarkup GetReplyMarkup(this PollMessage message)
    {
      var pollReplyMarkup = message.Poll.GetReplyMarkup();

      if ((pollReplyMarkup == null) || (message.ChatId == null) || (message.Poll.Owner != message.ChatId))
        return pollReplyMarkup;

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