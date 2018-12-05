using System;
using JetBrains.Annotations;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MeansImplicitUse]
  public class MessageTypeAttribute : Attribute, IHandlerAttribute<Message, PollMessage>
  {
    public MessageType MessageType { get; set; }

    public bool ShouldProcess(Message message, PollMessage context)
    {
      return message.Type == MessageType;
    }
  }
}