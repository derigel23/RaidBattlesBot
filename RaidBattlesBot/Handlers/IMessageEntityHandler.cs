using System;
using JetBrains.Annotations;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  public interface IMessageEntityHandler : IHandler<MessageEntityEx, PollMessage, bool?>
  {
    
  }

  [MeansImplicitUse]
  public class MessageEntityTypeAttribute : Attribute, IHandlerAttribute<MessageEntityEx, PollMessage>
  {
    public MessageEntityType EntityType { get; set; }

    public bool ShouldProcess(MessageEntityEx messageEntity, PollMessage pollMessage)
    {
      return messageEntity.Type == EntityType;
    }
  }
}