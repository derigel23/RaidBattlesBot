using System;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Model
{
  public class PollMessage : ITrackable
  {
    public PollMessage() {}

    public PollMessage(Message message)
    {
      UserId = message.From.Id;
      Chat = message.Chat;
      MesssageId = message.MessageId;
    }

    public PollMessage(ChosenInlineResult inlineResult)
    {
      UserId = inlineResult.From.Id;
      InlineMesssageId = inlineResult.InlineMessageId;
    }

    public int Id { get; set; }
    public int PollId { get; set; }
    public int UserId { get; set; }
    public long? ChatId { get; set; }
    public int? MesssageId { get; set; }
    public string InlineMesssageId { get; set; }
    public DateTimeOffset? Modified { get; set; }

    public ChatId Chat
    {
      get => ChatId.HasValue ?  new ChatId(ChatId.Value) : null;
      set => ChatId = value?.Identifier;
    }
  }
}