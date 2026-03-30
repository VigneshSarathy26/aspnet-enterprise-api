namespace MyApp.Api.Middleware;

/// <summary>
/// Ensures every request carries a Correlation ID (X-Correlation-Id header).
/// Generates a new GUID if none is provided, and echoes it back in the response.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers[HeaderName].FirstOrDefault()
                         ?? Guid.NewGuid().ToString("N");

        ctx.Items[HeaderName] = correlationId;
        ctx.Response.Headers.TryAdd(HeaderName, correlationId);

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
            await _next(ctx);
    }
}
