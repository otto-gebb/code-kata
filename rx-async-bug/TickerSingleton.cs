public class TickerSingleton : IDisposable
{
    private readonly ILogger<TickerSingleton> _logger;
    private bool _disposed = false;

    public TickerSingleton(ILogger<TickerSingleton> logger)
    {
        _logger = logger;
    }

    public void Tick(long i)
    {
        if (_disposed)
        {
            Console.WriteLine($"BOOM! dependency is called after disposal");
            throw new ObjectDisposedException(nameof(TickerSingleton));
        }

        Console.WriteLine($"tick {i}");
    }

    public void Dispose()
    {
        _logger.LogInformation("TickerSingleton is being disposed");
        _disposed = true;
    }
}