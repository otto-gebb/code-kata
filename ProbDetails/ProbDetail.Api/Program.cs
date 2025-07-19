using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// Optional: Replace the default ProblemDetails writers with a custom one that always returns JSON, unlike the writer
// registered by `AddProblemDetails` that only gets enabled when the request has a matching `Accept` header.
// builder.Services.AddSingleton<IProblemDetailsWriter, ProbDetail.Utils.JsonProblemDetailsWriter>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProbDetail.Utils.DefaultExceptionHandler>();

// Respond with ProblemDetails on JSON binding errors.
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
// Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware always logs the exception as an error (before .net10).
// So we should increase its log level to Critical to disable such logging.
// E.g. in appsettings.json:
/*  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware": "Critical",
    }
  },
*/
app.UseExceptionHandler();

// Example 1: Generic exception that will be handled by ProbDetail.Utils.DefaultExceptionHandler.
app.MapGet("/exception", () =>
{
    throw new InvalidOperationException("Sample Exception");
});

// Example 2: Custom exception carrying ProblemDetails.
app.MapGet("/not-found", () =>
{
    throw new ProbDetail.Utils.ProblemDetailsException(new ProblemDetails
    {
        Title = "Not found",
        Detail = "The requested book was not found",
        Status = StatusCodes.Status404NotFound,
        Type = "urn:acme-corp:errors:not-found"
    });
});

// Example 3: Multiple errors for one field.
app.MapPost("/books", (BookDto book) =>
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(book.Title))
        errors["Title"] = ["Title is required."];
    if (book.Year < 0 || book.Year > DateTime.Now.Year)
        errors["Year"] = [$"Year must be between 0 and {DateTime.Now.Year}."];

    var authorErrors = new List<string>();
    if (string.IsNullOrEmpty(book.Author))
        authorErrors.Add("Author is required.");
    else {
        if (string.IsNullOrWhiteSpace(book.Author))
            authorErrors.Add("Author cannot be only whitespace.");
        if (book.Author.Trim().Length < 2)
            authorErrors.Add("Author must be at least 2 characters long." );
    }
    if (authorErrors.Count > 0)
        errors["Author"] = authorErrors.ToArray();

    if (errors.Count > 0)
        return Results.ValidationProblem(
            errors,
            title: "Bad Request",
            detail: "One or more validation errors occurred.",
            type: "urn:acme-corp:department-x:errors:bad-request");

    // In a real app, you would save the book to a database here
    return Results.Created($"/books/{Guid.NewGuid()}", book);
})
.WithName("CreateBook");

// Example 4: Generic validation error associated with the conventional field "_" (underscore).
app.MapPost("/cd-player/next", static () =>
{
    var errors = new Dictionary<string, string[]>();
    errors["_"] = ["errors.cannot_play_next_when_no_cd_inserted"];

    if (errors.Count > 0)
        return Results.Problem(
            type: "urn:acme-corp:department-x:errors:bad-request",
            statusCode: StatusCodes.Status400BadRequest,
            detail: "Cannot play next track when no CD is inserted.",
            extensions: [new KeyValuePair<string, object?>("errors", errors)]);

    return Results.Ok();
})
.WithName("PlayNextTrack");

// Example 5: Custom `localizedDetail` extension with a localization resource key as the value.
app.MapPost("/cd-player/previous", static () =>
{
    var extensions = new Dictionary<string, object?>();
    extensions["localizedDetail"] = "errors.cannot_play_previous_when_no_cd_inserted";

    return Results.Problem(
        type: "urn:acme-corp:department-x:errors:bad-request",
        statusCode: StatusCodes.Status400BadRequest,
        detail: "Cannot play previous track when no CD is inserted.",
        extensions: extensions);
})
.WithName("PlayPreviousTrack");

app.Run();

record BookDto(string Title, int Year, string Author);
