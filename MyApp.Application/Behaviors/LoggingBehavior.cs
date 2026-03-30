using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MyApp.Application.Behaviors;

/// <summary>
/// Logs entry, exit, and elapsed time for every MediatR request.
/// Warning is emitted when execution exceeds a configurable threshold.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly TimeSpan SlowRequestThreshold = TimeSpan.FromMilliseconds(500);

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        var sw   = Stopwatch.StartNew();

        _logger.LogDebug("Executing {RequestName}", name);

        try
        {
            var response = await next();
            sw.Stop();

            if (sw.Elapsed > SlowRequestThreshold)
                _logger.LogWarning("Slow request detected: {RequestName} took {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            else
                _logger.LogDebug("Completed {RequestName} in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Request {RequestName} failed after {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
