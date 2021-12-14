using System.Threading;
using System.Threading.Tasks;

namespace RaidBattlesBot;

public interface IBackgroundServiceWorker
{
  Task Execute(CancellationToken cancellationToken);
}