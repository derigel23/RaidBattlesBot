using System;
using JetBrains.Annotations;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RaidBattlesBot.Handlers
{
  public interface IChosenInlineResultHandler : IHandler<ChosenInlineResult, object, bool>
  {
    
  }
}