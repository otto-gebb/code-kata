using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ProbDetail.Utils;

/// <summary>
/// Converts an unhandled exception in the ASP.NET Core pipeline to a `ProblemDetails` response.
/// </summary>
public class DefaultExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment environment;
    private readonly ILogger<DefaultExceptionHandler> logger;

    public DefaultExceptionHandler(
        IHostEnvironment environment,
        ILogger<DefaultExceptionHandler> logger
    )
    {
        this.environment = environment;
        this.logger = logger;
    }

    /// <summary>
    /// Handles the specified exception.
    /// </summary>
    /// <param name="httpContext">
    /// The <see cref="T:Microsoft.AspNetCore.Http.HttpContext" /> for the request.
    /// </param>
    /// <param name="exception">The unhandled exception.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// <see langword="true" /> if the exception was handled successfully; otherwise <see langword="false" />.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var exceptionHandlerFeature =
            httpContext.Features.GetRequiredFeature<IExceptionHandlerFeature>();
        ProblemDetails problemDetails = HandleException(httpContext, exception);
        ProblemHttpResult typedResult = TypedResults.Problem(problemDetails);

        // Uses IProblemDetailsService under the hood to write the ProblemDetails object, or
        // just writes it as JSON if no writer can handle it.
        await typedResult.ExecuteAsync(httpContext);
        return true;
    }

    private ProblemDetails HandleException(HttpContext httpContext, Exception exception)
    {
        ProblemDetails details;
        switch (exception)
        {
            case ProblemDetailsException pde:
                logger.LogDebug(pde, "ProblemDetailsException occurred");
                if (pde.ProblemDetails.Status is { } status)
                {
                    httpContext.Response.StatusCode = status;
                }
                else
                {
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }

                details = pde.ProblemDetails;
                break;
            case BadHttpRequestException bre:
                logger.LogWarning(bre.Message);
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                details = new ProblemDetails
                {
                    Title = "Bad Request",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = bre.Message,
                };
                break;
            default:
                logger.LogError(exception, "An unhandled exception occurred");
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                details = new ProblemDetails
                {
                    Type = "about:blank",
                    Title = "Internal server error",
                    Status = StatusCodes.Status500InternalServerError,
                };
                if (environment.IsDevelopment())
                {
                    details.Detail = exception.Message;
                    details.Extensions["trace"] = exception.StackTrace;
                }
                break;
        }

        details.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        return details;
    }
}
