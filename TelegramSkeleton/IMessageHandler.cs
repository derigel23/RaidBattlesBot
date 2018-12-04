using System;
using JetBrains.Annotations;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  public interface IMessageHandler<in TContext> : IHandler<Message, TContext, bool?>
  {
  }

  [MeansImplicitUse]
  public class MessageTypeAttribute : Attribute, IHandlerAttribute<Message>
  {
    public MessageType MessageType { get; set; }

    public bool ShouldProcess(Message message)
    {
      return message.Type == MessageType;
    }
  }
}