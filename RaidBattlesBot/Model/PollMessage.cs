using System;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Model
{
  public class PollMessage : ITrackable
  {
    public PollMessage() {}

    public PollMessage(Message message)
    {
      UserId = message.From?.Id;
      Chat = message.Chat;
    }

    public PollMessage(ChosenInlineResult inlineResult)
    {
      UserId = inlineResult.From?.Id;
      InlineMesssageId = inlineResult.InlineMessageId;
    }

    public PollMessage(InlineQuery inlineResult)
    {
      UserId = inlineResult.From?.Id;
    }

    public PollMessage(CallbackQuery callbackQuery)
    {
      UserId = callbackQuery.From?.Id;
      InlineMesssageId = callbackQuery.InlineMessageId;
      Chat = callbackQuery.Message?.Chat;
      MesssageId = callbackQuery.Message?.MessageId;
    }

    public int Id { get; set; }
    public int PollId { get; set; }
    public int? UserId { get; set; }
    public long? ChatId { get; set; }
    public ChatType? ChatType { get; set; }
    public int? MesssageId { get; set; }
    public string InlineMesssageId { get; set; }
    public PollMode? PollMode { get; set; }
    public DateTimeOffset? Modified { get; set; }

    public Poll Poll { get; set; }

    public Chat Chat
    {
      get => ChatId is { } chatId ? new Chat { Id = chatId, Type = ChatType.GetValueOrDefault() } : null;
      set { ChatId = value?.Id; ChatType = value?.Type; }
    }
  }
}