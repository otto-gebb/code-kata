using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace ProbDetail.Utils;

/// <summary>
/// Write a <see cref="T:Microsoft.AspNetCore.Mvc.ProblemDetails" />
/// payload to the current <see cref="P:Microsoft.AspNetCore.Http.HttpContext.Response" />.
/// </summary>
/// <remarks>
/// Based on DefaultProblemDetailsWriter from ASP.Net
/// see https://github.com/dotnet/aspnetcore/blob/v9.0.2/src/Http/Http.Extensions/src/DefaultProblemDetailsWriter.cs
/// which enables itself only if the client requested JSON in the Accept header.
/// This is too conservative when all APIs respond with JSON by default.
/// 
/// To make sure this writer replaces the default writers, register it in the service collection
/// before `AddControllers` and `AddProblemDetails`.
/// </remarks>
public class JsonProblemDetailsWriter : IProblemDetailsWriter
{
    private readonly ProblemDetailsOptions options;
    private readonly JsonSerializerOptions serializerOptions;

    public JsonProblemDetailsWriter(IOptions<ProblemDetailsOptions> options, IOptions<JsonOptions> jsonOptions)
    {
        this.options = options.Value;
        serializerOptions = jsonOptions.Value.SerializerOptions;
    }

    public bool CanWrite(ProblemDetailsContext context) =>
        // We may not satisfy the client's Accept header, but that's OK, we are not required to.
        true;

    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        HttpContext httpContext = context.HttpContext;
        options.CustomizeProblemDetails?.Invoke(context);
        Type problemDetailsType = context.ProblemDetails.GetType();
        return new ValueTask(
            httpContext.Response.WriteAsJsonAsync(
                context.ProblemDetails,
                serializerOptions.GetTypeInfo(problemDetailsType),
                "application/problem+json"
            )
        );
    }
}