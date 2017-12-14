using System;
using JetBrains.Annotations;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  public interface IMessageEntityHandler : IHandler<MessageEntity, object, Message>
  {
    
  }

  [MeansImplicitUse]
  public class MessageEntityTypeAttribute : Attribute
  {
    public MessageEntityType EntityType { get; }

    public MessageEntityTypeAttribute(MessageEntityType entityType)
    {
      EntityType = entityType;
    }
  }
}