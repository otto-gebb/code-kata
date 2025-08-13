public class TaskWorker : BackgroundService
{
    private readonly TickerSingleton _ticker;
    private readonly ILogger<TaskWorker> _logger;

    public TaskWorker(TickerSingleton ticker, ILogger<TaskWorker> logger)
    {
        _ticker = ticker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker starting up");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        int i = 0;
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Even if the task doesn't complete immediately on cancellation,
            // we still wait for it to finish.
            await DoWorkAsync(++i, stoppingToken);
        }
    }

    public async Task DoWorkAsync(long i, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        // Simulate some work that can't be cancelled.
        await Task.Delay(2000);
        _ticker.Tick(i);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker shutting down");
        return base.StopAsync(cancellationToken);
    }
}