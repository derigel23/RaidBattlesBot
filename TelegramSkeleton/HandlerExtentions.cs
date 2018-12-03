using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;

namespace RaidBattlesBot.Handlers
{
  public static class HandlerExtentions
  {
    public static IEnumerable<T> Bind<TParameter, T>(this IEnumerable<Func<TParameter, T>> items, TParameter parameter)
    {
      return items.Select(func => func(parameter));
    }

    public static IEnumerable<Meta<Func<T>, TMetadata>> Bind<TParameter, T, TMetadata>(this IEnumerable<Meta<Func<TParameter, T>, TMetadata>> items, TParameter parameter)
    {
      return items.Select(meta => new Meta<Func<T>, TMetadata>(() => meta.Value(parameter), meta.Metadata));
    }
  }

  public static class HandlerExtentions<TResult>
  {
    public static async Task<TResult> Handle<TData, TContext>(IEnumerable<IHandler<TData, TContext, TResult>> handlers, TData data, TContext context = default, CancellationToken cancellationToken = default)
    {
      foreach (var handler in handlers)
      {
        var result = await handler.Handle(data, context, cancellationToken);
        if (!EqualityComparer<TResult>.Default.Equals(result, default))
          return result;
      }

      return default;
    }

    public static async Task<TResult> Handle<THandler, TData, TContext, TMetadata>(IEnumerable<Meta<Func<THandler>, TMetadata>> handlers, TData data, TContext context = default, CancellationToken cancellationToken = default)
      where THandler : IHandler<TData, TContext, TResult>
      where TMetadata : Attribute, IHandlerAttribute<TData>
    {
      foreach (var h in handlers)
      {
        if (!h.Metadata.ShouldProcess(data))
          continue;
        var result = await h.Value().Handle(data, context, cancellationToken);
        if (!EqualityComparer<TResult>.Default.Equals(result, default))
          return result;
      }

      return default;
    }
  }
}