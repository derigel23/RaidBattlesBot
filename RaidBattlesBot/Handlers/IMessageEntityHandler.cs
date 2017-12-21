using System;
using JetBrains.Annotations;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  public interface IMessageEntityHandler : IHandler<MessageEntity, Raid, bool?>
  {
    
  }

  [MeansImplicitUse]
  public class MessageEntityTypeAttribute : Attribute, IHandlerAttribute<MessageEntity, Raid>
  {
    public MessageEntityType EntityType { get; set; }

    public bool ShouldProcess(MessageEntity messageEntity, Raid raid)
    {
      return messageEntity.Type == EntityType;
    }
  }
}