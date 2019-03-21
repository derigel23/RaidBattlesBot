using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  [UpdateHandler(UpdateType = UpdateType.ChosenInlineResult)]
  public class ChosenInlineResultUpdateHandler : IUpdateHandler<bool?>
  {
    private readonly IEnumerable<IChosenInlineResultHandler> myChosenInlineResultHandlers;

    public ChosenInlineResultUpdateHandler(IEnumerable<IChosenInlineResultHandler> chosenInlineResultHandlers)
    {
      myChosenInlineResultHandlers = chosenInlineResultHandlers;
    }
    
    public async Task<bool?> Handle(Update update, OperationTelemetry telemetry, CancellationToken cancellationToken = default)
    {
      var chosenInlineResult = update.ChosenInlineResult;
      telemetry.Properties["uid"] = chosenInlineResult.From?.Username;
      telemetry.Properties["query"] = chosenInlineResult.Query;
      telemetry.Properties["result"] = chosenInlineResult.ResultId;
      return await HandlerExtentions<bool?>.Handle(myChosenInlineResultHandlers, chosenInlineResult, new object(), cancellationToken).ConfigureAwait(false);
    }
  }
}