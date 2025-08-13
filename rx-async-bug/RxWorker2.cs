using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

public class RxWorker2 : BackgroundService
{
    private readonly TickerSingleton _ticker;
    private readonly ILogger<RxWorker2> _logger;

    public RxWorker2(TickerSingleton ticker, ILogger<RxWorker2> logger)
    {
        _ticker = ticker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker starting up");

        await Observable.Interval(TimeSpan.FromSeconds(1))
            .SelectMany(i => Observable.FromAsync((CancellationToken ct) => DoWorkAsync(i, ct)))
            // This unsubscribes when the cancellation token is triggered, but
            // it does wait for the task returned by `DoWorkAsync` to complete.
            .ToTask(stoppingToken);
    }

    public async Task DoWorkAsync(long i, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        // Simulate some work that can't be cancelled.
        await Task.Delay(1000);
        _ticker.Tick(i);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker shutting down");
        return base.StopAsync(cancellationToken);
    }
}