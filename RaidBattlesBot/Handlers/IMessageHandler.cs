using System;
using JetBrains.Annotations;
using RaidBattlesBot.Model;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  public interface IMessageHandler : IHandler<Message, Raid, bool?>
  {
    
  }

  [MeansImplicitUse]
  public class MessageTypeAttribute : Attribute, IHandlerAttribute<Message, Raid>
  {
    public MessageType MessageType { get; set; }

    public bool ShouldProcess(Message message, Raid raid)
    {
      return message.Type == MessageType;
    }
  }
}