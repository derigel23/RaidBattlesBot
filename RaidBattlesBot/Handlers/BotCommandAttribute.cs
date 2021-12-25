using System;
using JetBrains.Annotations;
using RaidBattlesBot.Model;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{ 
  public interface IBotCommandHandler :  IMessageEntityHandler, IBotCommandHandler<PollMessage, bool?> { }
  
  [MeansImplicitUse]
  [BaseTypeRequired(typeof(IBotCommandHandler))]
  public class BotCommandAttribute : MessageEntityTypeAttribute, IBotCommandHandlerAttribute<PollMessage>
  {
    [UsedImplicitly] public BotCommandAttribute() { }

    public BotCommandAttribute(string command, string description, params BotCommandScopeType[] commandScopeTypes)
    {
      EntityType = MessageEntityType.BotCommand;
      Command = new BotCommand { Command = command, Description = description };
      Scopes = Array.ConvertAll(commandScopeTypes, BotCommandHandler.GetScope);
    }

    public BotCommandAttribute(string command, string description)
      : this(command, description, BotCommandHandler.SupportedBotCommandScopeTypes)
    {
    }

    public override bool ShouldProcess(MessageEntityEx data, PollMessage context)
    {
      return BotCommandHandler.ShouldProcess(this, data, context);
    }

    public BotCommandScope[] Scopes { get; set; }
    public BotCommand Command { get; set; }
    public string[] Aliases { get; set; }
  }
}