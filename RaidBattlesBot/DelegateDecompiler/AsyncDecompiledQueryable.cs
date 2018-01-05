using System;
using System.Collections.Generic;
using System.Linq;

namespace DelegateDecompiler.EntityFramework
{
  class AsyncDecompiledQueryable<T> : DecompiledQueryable<T>, IAsyncEnumerable<T>
  {
    private readonly IQueryable<T> inner;

    protected internal AsyncDecompiledQueryable(IQueryProvider provider, IQueryable<T> inner)
      : base(provider, inner)
    {
      this.inner = inner;
    }

    public IAsyncEnumerator<T> GetEnumerator()
    {
      if (!(inner is IAsyncEnumerable<T> asyncEnumerable))
      {
        throw new InvalidOperationException("The source IQueryable doesn't implement IDbAsyncEnumerable<T>.");
      }
      return asyncEnumerable.GetEnumerator();
    }
  }
}