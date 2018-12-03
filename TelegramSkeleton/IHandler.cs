using System.Threading;
using System.Threading.Tasks;

namespace RaidBattlesBot.Handlers
{
  public interface IHandler<in TData, in TContext, TResult>
  {
    Task<TResult> Handle(TData data, TContext context = default, CancellationToken cancellationToken = default);
  }
}