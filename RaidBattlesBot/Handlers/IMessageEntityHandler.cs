using System;
using System.ComponentModel;
using JetBrains.Annotations;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  public interface IMessageEntityHandler : IMessageEntityHandler<PollMessage, bool?> { }
  
  [MeansImplicitUse]
  [BaseTypeRequired(typeof(IMessageEntityHandler))]
  public class MessageEntityTypeAttribute : DescriptionAttribute, IHandlerAttribute<MessageEntityEx, PollMessage>
  {
    public MessageEntityTypeAttribute() { }
    public MessageEntityTypeAttribute(string description) : base(description) { }
    
    public MessageEntityType EntityType { get; set; }

    public bool ShouldProcess(MessageEntityEx messageEntity, PollMessage context)
    {
      return messageEntity.Type == EntityType;
    }

    public int Order { get; set; }
    
    int IHandlerAttribute<MessageEntityEx, PollMessage>.Order => (int)EntityType * 100 + Order;
  }
}