using System;
using System.Linq;
using JetBrains.Annotations;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  public interface IMessageHandler : IMessageHandler<PollMessage, bool?> { }
  
  [MeansImplicitUse]
  public class MessageTypeAttribute : Attribute, IHandlerAttribute<Message, (UpdateType updateType, PollMessage pollMessage)>
  {
    // by default only for new messages
    public MessageTypeAttribute() : this(UpdateType.Message, UpdateType.ChannelPost) { }

    public MessageTypeAttribute(params UpdateType[] updateTypes)
    {
      UpdateTypes = updateTypes;
    }
    
    public MessageType MessageType { get; set; }

    public UpdateType[] UpdateTypes { get; set; }
    
    public bool ShouldProcess(Message message, (UpdateType updateType, PollMessage pollMessage) context)
    {
      return UpdateTypes.Contains(context.updateType) && message.Type == MessageType;
    }

    public int Order => (int) MessageType;
  }
}