namespace kecas.application.abstractions.behaviors;

using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Logs every command/query with structured fields. Requests carry a name so
/// logs can be correlated without leaking request payloads (§11.4: no
/// passwords or sensitive values in logs).
/// </summary>
internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next();
            logger.LogInformation("Handled {RequestName}", requestName);
            return response;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed {RequestName}", requestName);
            throw;
        }
    }
}
