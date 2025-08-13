using System.Reactive.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<TickerSingleton>();
//builder.Services.AddHostedService<RxWorker1>();
builder.Services.AddHostedService<RxWorker2>();
//builder.Services.AddHostedService<TaskWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.MapGet("/ping", () =>
{
    return "Pong";
});

app.Run();

// Simulate additional cleanup.
await Task.Delay(2000);