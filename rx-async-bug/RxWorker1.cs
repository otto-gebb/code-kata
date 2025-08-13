using System.Reactive.Linq;

public class RxWorker1 : BackgroundService
{
    private readonly TickerSingleton _ticker;
    private readonly ILogger<RxWorker1> _logger;
    private IDisposable? _subscription;

    public RxWorker1(TickerSingleton ticker, ILogger<RxWorker1> logger)
    {
        _ticker = ticker;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker starting up");

        _subscription = Observable.Interval(TimeSpan.FromSeconds(1))
            .SelectMany(i => Observable.FromAsync((CancellationToken ct) => DoWorkAsync(i, ct)))
            .Subscribe(
                onNext: _ => { },
                // OnError will not be called after we unsubscribe.
                onError: ex => _logger.LogError(ex, "An error occurred in the worker"));
        return Task.CompletedTask;
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
        _subscription?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}