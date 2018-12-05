using System;
using JetBrains.Annotations;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  [MeansImplicitUse]
  public class MessageEntityTypeAttribute : Attribute, IHandlerAttribute<MessageEntityEx, PollMessage>
  {
    public MessageEntityType EntityType { get; set; }

    public bool ShouldProcess(MessageEntityEx messageEntity, PollMessage context)
    {
      return messageEntity.Type == EntityType;
    }
  }
}