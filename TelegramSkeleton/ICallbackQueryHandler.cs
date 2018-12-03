using System;
using JetBrains.Annotations;
using Telegram.Bot.Types;

namespace Team23.TelegramSkeleton
{
  public interface ICallbackQueryHandler : IHandler<CallbackQuery, object, (string text, bool showAlert, string url)>
  {
    
  }

  [MeansImplicitUse]
  public class CallbackQueryHandlerAttribute : Attribute, IHandlerAttribute<CallbackQuery>
  {
    public string DataPrefix { get; set; }

    public bool ShouldProcess(CallbackQuery callbackQuery)
    {
      return callbackQuery.Data.StartsWith(DataPrefix);
    }
  }
}