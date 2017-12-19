using System;
using JetBrains.Annotations;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  public interface IMessageEntityHandler : IHandler<MessageEntity, object, bool>
  {
    
  }

  [MeansImplicitUse]
  public class MessageEntityTypeAttribute : Attribute, IHandlerAttribute<MessageEntity, object>
  {
    public MessageEntityType EntityType { get; set; }

    public bool ShouldProcess(MessageEntity messageEntity, object context)
    {
      return messageEntity.Type == EntityType;
    }
  }
}