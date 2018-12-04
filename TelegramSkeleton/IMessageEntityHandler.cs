using System;
using JetBrains.Annotations;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  public interface IMessageEntityHandler<in TContext> : IHandler<MessageEntityEx, TContext, bool?>
  {
    
  }

  [MeansImplicitUse]
  public class MessageEntityTypeAttribute : Attribute, IHandlerAttribute<MessageEntityEx>
  {
    public MessageEntityType EntityType { get; set; }

    public bool ShouldProcess(MessageEntityEx messageEntity)
    {
      return messageEntity.Type == EntityType;
    }
  }
}