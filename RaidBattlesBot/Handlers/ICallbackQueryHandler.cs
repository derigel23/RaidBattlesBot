using System;
using JetBrains.Annotations;
using Team23.TelegramSkeleton;
using Telegram.Bot.Types;

namespace RaidBattlesBot.Handlers
{
  public interface ICallbackQueryHandler : ICallbackQueryHandler<object>
  {
  }
  
  [MeansImplicitUse]
  [BaseTypeRequired(typeof(ICallbackQueryHandler))]
  public class CallbackQueryHandlerAttribute : Attribute, IHandlerAttribute<CallbackQuery, object>
  {
    public string DataPrefix { get; set; }

    public bool ShouldProcess(CallbackQuery callbackQuery, object context)
    {
      return callbackQuery.Data?.StartsWith(DataPrefix) ?? false;
    }

    public int Order => string.IsNullOrEmpty(DataPrefix) ? int.MaxValue : 0;
  }
}