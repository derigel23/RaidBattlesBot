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
  public class BotBotCommandAttribute : MessageEntityTypeAttribute, IBotCommandHandlerAttribute<PollMessage>
  {
    [UsedImplicitly] public BotBotCommandAttribute() { }

    public BotBotCommandAttribute(string command, string description)
    {
      EntityType = MessageEntityType.BotCommand;
      Command = new BotCommand { Command = command, Description = description };
    }

    public BotBotCommandAttribute(string command, string description, BotCommandScopeType commandScopeType)
      : this(command, description)
    {
      Scope = BotCommandHandler.GetScope(commandScopeType);
    }
    
    public BotCommandScope Scope { get; set; }
    public BotCommand Command { get; set; }
  }
}