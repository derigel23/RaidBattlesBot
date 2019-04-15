using Microsoft.Extensions.Primitives;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  public class MessageEntityEx : MessageEntity
  {
    public MessageEntityEx(Message message, MessageEntity messageEntity)
    {
      Message = message;
      Type = messageEntity.Type;
      Offset = messageEntity.Offset;
      Length = messageEntity.Length;
      Url = messageEntity.Url;
      User = messageEntity.User;
      
      Value = new StringSegment(Message.Text, Offset, Length);
      AfterValue = new StringSegment(Message.Text, Offset + Length, Message.Text.Length - Offset - Length);

      if (Type == MessageEntityType.BotCommand && Value.IndexOf('@') is var atOffset)
      {
        if (atOffset >= 0)
        {
          Command = Value.Subsegment(0, atOffset);
          CommandBot = Value.Subsegment(atOffset + 1);
        }
        else
        {
          Command = Value;
        }
      }
    }

    public Message Message { get; }

    public StringSegment Value { get; }
    public StringSegment AfterValue { get; }

    public StringSegment Command { get; }
    public StringSegment CommandBot { get; }
  }
}