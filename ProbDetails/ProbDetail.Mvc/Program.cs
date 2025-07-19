using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

var builder = WebApplication.CreateBuilder(args);
// AddProblemDetails must come before AddControllers, see
// https://github.com/dotnet/aspnetcore/issues/59052
builder.Services.AddProblemDetails();
builder.Services.AddControllers(options =>
{
    // This sets the default response content type to "application/json" for all endpoints.
    // Can be overridden on a specific endpoint using the `ProducesAttribute`, e.g.
    // [Produces(MediaTypeNames.Application.Json, MediaTypeNames.Application.Xml)]
    options.Filters.Add(new ProducesAttribute(MediaTypeNames.Application.Json));
});
builder.Services.AddExceptionHandler<ProbDetail.Utils.DefaultExceptionHandler>();
var app = builder.Build();
app.UseRouting();
app.UseExceptionHandler();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
