using System;
using JetBrains.Annotations;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  public interface ICallbackQueryHandler : IHandler<CallbackQuery, object, (string text, bool showAlert, string url)>
  {
    
  }

  [MeansImplicitUse]
  public class CallbackQueryHandlerAttribute : Attribute, IHandlerAttribute<CallbackQuery, object>
  {
    public string DataPrefix { get; set; }

    public bool ShouldProcess(CallbackQuery callbackQuery, object context)
    {
      return callbackQuery.Data.StartsWith(DataPrefix);
    }
  }
}