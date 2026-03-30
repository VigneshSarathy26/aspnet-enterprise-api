using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MyApp.Api.Middleware;

/// <summary>
/// Global exception handler. Maps exceptions to RFC 9457 Problem Details responses.
/// Validation errors return 400; unhandled exceptions return 500.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed: {Errors}",
                string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));

            ctx.Response.StatusCode  = StatusCodes.Status400BadRequest;
            ctx.Response.ContentType = "application/problem+json";

            var problem = new ValidationProblemDetails(
                ex.Errors.GroupBy(e => e.PropertyName)
                         .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Title  = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Instance = ctx.Request.Path
            };

            await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Bad request: {Message}", ex.Message);
            await WriteProblemAsync(ctx, StatusCodes.Status400BadRequest, "Bad Request", ex.Message);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Not supported: {Message}", ex.Message);
            await WriteProblemAsync(ctx, StatusCodes.Status400BadRequest, "Not Supported", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(ctx, StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please try again or contact support.");
        }
    }

    private static Task WriteProblemAsync(HttpContext ctx, int status, string title, string detail)
    {
        ctx.Response.StatusCode  = status;
        ctx.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status   = status,
            Title    = title,
            Detail   = detail,
            Instance = ctx.Request.Path
        };

        return ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
