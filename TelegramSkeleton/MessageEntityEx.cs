using Microsoft.Extensions.Primitives;
using Telegram.Bot.Types;

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
    }

    public Message Message { get; }

    public StringSegment Value { get; }
    public StringSegment AfterValue { get; }
  }
}