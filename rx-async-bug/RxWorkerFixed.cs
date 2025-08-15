using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;

public class RxWorkerFixed : BackgroundService
{
    private readonly TickerSingleton _ticker;
    private readonly ILogger<RxWorkerFixed> _logger;
    private readonly Subject<Unit> _abortSubject = new Subject<Unit>();

    public RxWorkerFixed(TickerSingleton ticker, ILogger<RxWorkerFixed> logger)
    {
        _ticker = ticker;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker starting up");
        return Observable.Interval(TimeSpan.FromSeconds(1))
            // Complete the timer observable when shutting down instead of unsubscribing.
            .TakeUntil(_abortSubject)
            .SelectMany(i => Observable.FromAsync(_ => DoWorkAsync(i, stoppingToken)))
            // Do NOT pass stoppingToken to ToTask: cancellation causes unsubscription, in which case
            // tasks started by Observable.FromAsync are not awaited.
            // ReSharper disable once MethodSupportsCancellation
            .ToTask();
    }

    public async Task DoWorkAsync(long i, CancellationToken cancellationToken)
    {
        // Simulate some work that can't be cancelled.
        await Task.Delay(2000);
        _ticker.Tick(i);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker shutting down");
        _abortSubject.OnNext(Unit.Default);
        return base.StopAsync(cancellationToken);
    }
}
