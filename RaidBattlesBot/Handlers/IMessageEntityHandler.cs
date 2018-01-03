using System;
using JetBrains.Annotations;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  public interface IMessageEntityHandler : IHandler<MessageEntity, PollMessage, bool?>
  {
    
  }

  [MeansImplicitUse]
  public class MessageEntityTypeAttribute : Attribute, IHandlerAttribute<MessageEntity, PollMessage>
  {
    public MessageEntityType EntityType { get; set; }

    public bool ShouldProcess(MessageEntity messageEntity, PollMessage pollMessage)
    {
      return messageEntity.Type == EntityType;
    }
  }
}