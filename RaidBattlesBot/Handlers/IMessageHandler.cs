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
  public class MessageTypeAttribute : Attribute
  {
    public MessageType MessageType { get; }

    public MessageTypeAttribute(MessageType messageType)
    {
      MessageType = messageType;
    }
  }
}