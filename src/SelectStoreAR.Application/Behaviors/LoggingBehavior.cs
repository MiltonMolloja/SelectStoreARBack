using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SelectStoreAR.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        logger.LogInformation(
            "Handling {RequestName}",
            requestName);

        Stopwatch stopwatch = Stopwatch.StartNew();

        TResponse response = await next(cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();

        logger.LogInformation(
            "Handled {RequestName} in {ElapsedMilliseconds}ms",
            requestName,
            stopwatch.ElapsedMilliseconds);

        return response;
    }
}
