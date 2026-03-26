using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using SelectStoreAR.Domain.Common;

namespace SelectStoreAR.WebAPI.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        (int statusCode, string title, object? errors) = exception switch
        {
            DomainException domainEx => (StatusCodes.Status400BadRequest, domainEx.Message, (object?)null),
            ValidationException validationEx => (
                StatusCodes.Status422UnprocessableEntity,
                "Validation failed",
                validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            InvalidOperationException invOpEx => (StatusCodes.Status404NotFound, invOpEx.Message, (object?)null),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", (object?)null),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            logger.LogWarning(exception, "Handled exception: {Message}", exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            new { title, errors, traceId = httpContext.TraceIdentifier },
            cancellationToken)
            .ConfigureAwait(false);

        return true;
    }
}
