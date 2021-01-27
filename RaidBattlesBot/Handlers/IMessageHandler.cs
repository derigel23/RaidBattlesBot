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
  [BaseTypeRequired(typeof(IMessageHandler))]
  public class MessageTypeAttribute : Attribute, IHandlerAttribute<Message, (UpdateType updateType, PollMessage pollMessage)>
  {
    // by default only for new messages
    public MessageTypeAttribute() : this(UpdateType.Message, UpdateType.ChannelPost) { }

    public const MessageType AllMessageTypes = (MessageType) Int32.MaxValue;
    
    public MessageTypeAttribute(params UpdateType[] updateTypes)
    {
      UpdateTypes = updateTypes;
    }
    
    public MessageType MessageType { get; set; }

    public UpdateType[] UpdateTypes { get; set; }
    
    public bool ShouldProcess(Message message, (UpdateType updateType, PollMessage pollMessage) context)
    {
      return UpdateTypes.Contains(context.updateType) && (MessageType == AllMessageTypes || message.Type == MessageType);
    }

    public int Order => (int) MessageType;
  }
}