using System;
using JetBrains.Annotations;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  public interface IMessageEntityHandler : IMessageEntityHandler<PollMessage, bool?> { }
  
  [MeansImplicitUse]
  [BaseTypeRequired(typeof(IMessageEntityHandler))]
  public class MessageEntityTypeAttribute : Attribute, IHandlerAttribute<MessageEntityEx, PollMessage>
  {
    public MessageEntityType EntityType { get; set; }

    public bool ShouldProcess(MessageEntityEx messageEntity, PollMessage context)
    {
      return messageEntity.Type == EntityType;
    }

    public int Order => (int) EntityType;
  }
}