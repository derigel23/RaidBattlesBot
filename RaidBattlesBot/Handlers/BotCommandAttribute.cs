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

    public BotBotCommandAttribute(string command, string description, BotCommandScopeType commandScopeType, params string[] aliases)
      : this(command, description)
    {
      Scope = BotCommandHandler.GetScope(commandScopeType);
      Aliases = aliases;
    }

    public override bool ShouldProcess(MessageEntityEx data, PollMessage context)
    {
      return BotCommandHandler.ShouldProcess(this, data, context);
    }

    public BotCommandScope Scope { get; set; }
    public BotCommand Command { get; set; }

    public string[] Aliases { get; set; }
  }
}