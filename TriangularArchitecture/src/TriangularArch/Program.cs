using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TriangularArch.App;
using TriangularArch.App.Dtos;
using TriangularArch.Domain;
using TriangularArch.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db")
);
builder.Services.AddSingleton<TransactionHelper>();
builder.Services.AddScoped<WeatherForecastService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGet(
        "/weatherforecast",
        async ([FromServices] WeatherForecastService weatherService) =>
        {
            WeatherForecastDto[] forecast = await weatherService.GetWeatherForecasts();
            return forecast;
        }
    )
    .WithName("GetWeatherForecast");
app.MapPost(
        "/weatherforecast",
        async ([FromServices] WeatherForecastService weatherService) =>
        {
            var forecast = await weatherService.CreateWeatherForecast();
            return Results.Created($"/weatherforecast/{forecast.Id}", forecast);
        }
    )
    .WithName("CreateWeatherForecast");

// Initialize the database with some data if it doesn't exist.
using (var scope = app.Services.CreateScope())
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await dbContextFactory.CreateDbContextAsync();
    // Use Migrations in a real application.
    db.Database.EnsureCreated();
}

app.Run();

namespace TriangularArch.App.Dtos
{
    public record WeatherForecastDto(Guid Id, DateOnly Date, int TemperatureC, string? Summary);
}

namespace TriangularArch.App
{
    using TriangularArch.App.Dtos;

    public class WeatherForecastService
    {
        private static readonly string[] _summaries = ["Freezing", "Chilly", "Mild", "Warm", "Hot"];
        private readonly TransactionHelper _transaction;

        public WeatherForecastService(TransactionHelper transactionHelper)
        {
            _transaction = transactionHelper;
        }

        public async Task<WeatherForecastDto[]> GetWeatherForecasts()
        {
            WeatherForecast[] entities = await _transaction.ExecuteInTransaction(async db =>
                await db.WeatherForecasts.ToArrayAsync()
            );
            return Array.ConvertAll(entities, ToDto);
        }

        public async Task<WeatherForecastDto> CreateWeatherForecast()
        {
            // In a real application, take parameters form a CreateWeatherForecastDto.
            var forecast = new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now),
                Random.Shared.Next(-20, 55),
                _summaries[Random.Shared.Next(_summaries.Length)]
            );
            await _transaction.ExecuteInTransaction(async db =>
            {
                db.WeatherForecasts.Add(forecast);
                await db.SaveChangesAsync();
            });
            return ToDto(forecast);
        }

        private static WeatherForecastDto ToDto(WeatherForecast forecast)
        {
            return new WeatherForecastDto(
                forecast.Id,
                forecast.Date,
                forecast.TemperatureC,
                forecast.Summary
            );
        }
    }
}

namespace TriangularArch.Persistence
{
    using TriangularArch.Domain;

    public class TransactionHelper
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public TransactionHelper(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task ExecuteInTransaction(Func<AppDbContext, Task> action)
        {
            _ = await ExecuteInTransaction(async db =>
            {
                await action(db);
                return 0;
            });
        }

        public async Task<T> ExecuteInTransaction<T>(Func<AppDbContext, Task<T>> action)
        {
            // This is a draft. In a real application, you would retry on transient failures.
            await using var context = await _contextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable
            );
            try
            {
                var result = await action(context);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<WeatherForecast> WeatherForecasts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WeatherForecast>().HasKey(wf => wf.Id);
            modelBuilder.Entity<WeatherForecast>().Property(wf => wf.Id).ValueGeneratedNever();
            base.OnModelCreating(modelBuilder);
        }
    }
}

namespace TriangularArch.Domain
{
    public class WeatherForecast
    {
        // Needed for EF. Do not use this constructor in your code.
        protected WeatherForecast() { }

        public WeatherForecast(DateOnly date, int temperatureC, string? summary)
        {
            Id = Guid.CreateVersion7();
            // TODO: Validation.
            Date = date;
            TemperatureC = temperatureC;
            Summary = summary;
        }

        public Guid Id { get; private set; }
        public DateOnly Date { get; private set; }
        public int TemperatureC { get; private set; }
        public string? Summary { get; private set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
