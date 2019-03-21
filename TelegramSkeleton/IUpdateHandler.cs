using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  public interface IUpdateHandler<TResult> : IHandler<Update, OperationTelemetry, TResult>
  {
  }
  
  [MeansImplicitUse]
  public class UpdateHandlerAttribute : Attribute, IHandlerAttribute<Update, OperationTelemetry>
  {
    public UpdateHandlerAttribute(params UpdateType[] supportedUpdateTypes)
    {
      UpdateTypes = new HashSet<UpdateType>(supportedUpdateTypes);  
    }
    
    public ISet<UpdateType> UpdateTypes { get; }

    public bool ShouldProcess(Update update, OperationTelemetry telemetry)
    {
      return UpdateTypes.Contains(update.Type);
    }
  }
}