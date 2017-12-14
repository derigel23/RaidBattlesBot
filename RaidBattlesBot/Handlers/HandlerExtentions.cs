using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

    public static IEnumerable<Meta<Func<T>>> Bind<TParameter, T>(this IEnumerable<Meta<Func<TParameter, T>>> items, TParameter parameter)
    {
      return items.Select(meta => new Meta<Func<T>>(() => meta.Value(parameter), meta.Metadata));
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

    public static async Task<TResult> Handle<THandler, TData, TContext, TAttribute, TFilter>(IEnumerable<Meta<Func<THandler>>> handlers, Expression<Func<TAttribute, TFilter>> attributeFilter, Expression<Func<TData, TFilter>> dataFilter, TData data, TContext context = default, CancellationToken cancellationToken = default)
      where THandler : IHandler<TData, TContext, TResult>
      where TAttribute : Attribute
    {
      foreach (var handler in handlers)
      {
        if (!Equals(handler.Metadata[PropertySupport.ExtractPropertyName(attributeFilter)], dataFilter.Compile().Invoke(data)))
          continue;
        var result = await handler.Value().Handle(data, context, cancellationToken);
        if (!EqualityComparer<TResult>.Default.Equals(result, default))
          return result;
      }

      return default;
    }
  }
}