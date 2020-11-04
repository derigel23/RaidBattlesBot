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
      InlineMessageId = inlineResult.InlineMessageId;
    }

    public PollMessage(InlineQuery inlineResult)
    {
      UserId = inlineResult.From?.Id;
    }

    public PollMessage(CallbackQuery callbackQuery)
    {
      UserId = callbackQuery.From?.Id;
      InlineMessageId = callbackQuery.InlineMessageId;
      Chat = callbackQuery.Message?.Chat;
      MessageId = callbackQuery.Message?.MessageId;
    }

    public int Id { get; set; }
    public int? BotId { get; set; }
    public int PollId { get; set; }
    public int? UserId { get; set; }
    public long? ChatId { get; set; }
    public ChatType? ChatType { get; set; }
    public int? MessageId { get; set; }
    public string InlineMessageId { get; set; }
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