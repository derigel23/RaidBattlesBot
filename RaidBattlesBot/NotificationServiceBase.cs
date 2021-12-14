using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RaidBattlesBot;

public abstract class NotificationServiceBase<TWorker> : BackgroundService
  where TWorker : IBackgroundServiceWorker
{
  private readonly TelemetryClient myTelemetryClient;
  private readonly IServiceProvider myServiceProvider;
  private readonly TimeSpan myPeriod;
  private readonly SemaphoreSlim mySemaphore;

  protected NotificationServiceBase(TelemetryClient telemetryClient, IServiceProvider serviceProvider, TimeSpan period)
  {
    myTelemetryClient = telemetryClient;
    myServiceProvider = serviceProvider;
    myPeriod = period;
    mySemaphore = new SemaphoreSlim(1, 1);
  }
  
  protected override Task ExecuteAsync(CancellationToken stoppingToken)
  {
    // ReSharper disable once AsyncVoidLambda
    var timer = new Timer(async o =>
    {
      var cancellationToken = (CancellationToken)o!;
      if (!await mySemaphore.WaitAsync(TimeSpan.Zero, cancellationToken)) return;
      try
      {
        using var scope = myServiceProvider.CreateScope();
        await (scope.ServiceProvider.GetService<TWorker>()?.Execute(cancellationToken) ?? Task.CompletedTask);
      }
      catch (Exception ex)
      {
        myTelemetryClient.TrackException(ex);
      }
      finally
      {
        mySemaphore.Release();
      }
    }, stoppingToken, TimeSpan.Zero, myPeriod);
    stoppingToken.Register(() => timer.Dispose());
    return Task.CompletedTask;
  }
}