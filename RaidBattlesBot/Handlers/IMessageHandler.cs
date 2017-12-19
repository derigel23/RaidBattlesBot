using System;
using JetBrains.Annotations;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  public interface IMessageHandler : IHandler<Message, object, bool>
  {
    
  }

  [MeansImplicitUse]
  public class MessageTypeAttribute : Attribute, IHandlerAttribute<Message, object>
  {
    public MessageType MessageType { get; set; }

    public bool ShouldProcess(Message message, object context)
    {
      return message.Type == MessageType;
    }
  }
}