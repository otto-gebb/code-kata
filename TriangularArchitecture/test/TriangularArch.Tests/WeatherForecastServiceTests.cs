using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TriangularArch.App;
using TriangularArch.App.Dtos;
using TriangularArch.Domain;
using TriangularArch.Persistence;

namespace TriangularArch.Tests;

public class TestDbContextFactory : IDbContextFactory<AppDbContext>, IDisposable
{
    private SqliteConnection? _connection;
    private DbContextOptions<AppDbContext> _options;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public AppDbContext CreateDbContext()
    {
        return new AppDbContext(_options);
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection = null;
    }
}

public class AppServiceTestBase : IDisposable
{
    private readonly TestDbContextFactory _dbContextFactory;

    protected AppServiceTestBase()
    {
        _dbContextFactory = new TestDbContextFactory();
        TransactionHelper = new TransactionHelper(_dbContextFactory);
        EnsureDbCreated();
    }

    protected TransactionHelper TransactionHelper { get; }

    public void WithDb(Action<AppDbContext> action)
    {
        WithDb(db =>
        {
            action(db);
            return true;
        });
    }

    public T WithDb<T>(Func<AppDbContext, T> action)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return action(db);
    }

    public virtual void Dispose()
    {
        _dbContextFactory.Dispose();
    }

    private void EnsureDbCreated()
    {
        WithDb(context =>
        {
            context.Database.EnsureCreated();
            return true;
        });
    }
}

public class WeatherForecastServiceTests : AppServiceTestBase
{
    private readonly WeatherForecastService _sut;

    public WeatherForecastServiceTests()
    {
        _sut = new WeatherForecastService(TransactionHelper);
    }

    [Fact]
    public async Task GetWeatherForecasts_ReadsDb()
    {
        WeatherForecast forecast = new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now),
            25,
            "Sunny"
        );
        WithDb(db =>
        {
            db.WeatherForecasts.Add(forecast);
            db.SaveChanges();
        });

        WeatherForecastDto[] dtos = await _sut.GetWeatherForecasts();

        WeatherForecastDto dto = Assert.Single(dtos);
        Assert.Equal(forecast.Id, dto.Id);
        Assert.Equal(forecast.Date, dto.Date);
    }

    [Fact]
    public async Task CreateWeatherForecast_UpdatesDb()
    {
        WeatherForecastDto created = await _sut.CreateWeatherForecast();
        WithDb(db =>
        {
            List<WeatherForecast> dbForecasts = db.WeatherForecasts.ToList();
            WeatherForecast forecast = Assert.Single(dbForecasts);
            Assert.NotNull(forecast);
            Assert.Equal(created.Id, forecast.Id);
            Assert.Equal(created.TemperatureC, forecast.TemperatureC);
        });
    }
}
