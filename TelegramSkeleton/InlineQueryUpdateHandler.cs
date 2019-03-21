using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  [UpdateHandler(UpdateType = UpdateType.InlineQuery)]
  public class InlineQueryUpdateHandler : IUpdateHandler<bool?>
  {
    private readonly IEnumerable<Meta<Func<IInlineQueryHandler>, InlineQueryHandlerAttribute>> myInlineQueryHandlers;

    public InlineQueryUpdateHandler(IEnumerable<Meta<Func<IInlineQueryHandler>, InlineQueryHandlerAttribute>> inlineQueryHandlers)
    {
      myInlineQueryHandlers = inlineQueryHandlers;
    }
    
    public async Task<bool?> Handle(Update update, OperationTelemetry telemetry, CancellationToken cancellationToken = default)
    {
      var inlineQuery = update.InlineQuery;
      telemetry.Properties["uid"] = inlineQuery.From?.Username;
      telemetry.Properties["query"] = inlineQuery.Query;
      return await HandlerExtentions<bool?>.Handle(myInlineQueryHandlers, inlineQuery, new object(), cancellationToken).ConfigureAwait(false);
    }
  }
}