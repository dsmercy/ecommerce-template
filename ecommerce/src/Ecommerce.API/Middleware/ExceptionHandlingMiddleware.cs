using System.Net;
using System.Text.Json;
using Ecommerce.Application.Common.Models;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Ecommerce.API.Middleware;

/// <summary>
/// Catches all unhandled exceptions and translates them to RFC-7807-style
/// JSON responses while writing structured log entries at two levels:
///
///   Warning  — validation errors, 401s, and 404s (business-as-usual failures).
///   Error    — unexpected exceptions with full stack trace and request context.
///
/// All entries from this middleware land in both <c>app-*.log</c> and the
/// dedicated <c>exceptions-*.log</c> sink (filtered by SourceContext in Program.cs).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors.Select(e => e.ErrorMessage).ToList();

            // Structured warning — individual validation failures are useful for diagnostics
            // but are expected traffic, not alerts.
            _logger.LogWarning(
                ex,
                "Validation failed | Path={Path} | Method={Method} | " +
                "RemoteIp={RemoteIp} | ErrorCount={ErrorCount} | Errors={Errors}",
                context.Request.Path,
                context.Request.Method,
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                errors.Count,
                string.Join("; ", errors));

            await WriteJsonResponseAsync(
                context,
                (int)HttpStatusCode.BadRequest,
                ApiResponse<object>.Fail("Validation failed.", errors));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                ex,
                "Unauthorized access | Path={Path} | Method={Method} | " +
                "RemoteIp={RemoteIp} | UserId={UserId} | Message={Message}",
                context.Request.Path,
                context.Request.Method,
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
                ex.Message);

            await WriteJsonResponseAsync(
                context,
                (int)HttpStatusCode.Unauthorized,
                ApiResponse<object>.Fail("Unauthorized."));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Resource not found | Path={Path} | Method={Method} | " +
                "RemoteIp={RemoteIp} | Message={Message}",
                context.Request.Path,
                context.Request.Method,
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                ex.Message);

            await WriteJsonResponseAsync(
                context,
                (int)HttpStatusCode.NotFound,
                ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            // ── Full structured error entry ────────────────────────────────────
            // Every unhandled exception is written with:
            //   • request coordinates  (method, path, query)
            //   • caller identity      (userId, remoteIp)
            //   • exception taxonomy   (type, message)
            //   • full stack trace     (via {ex} in the template)
            //
            // The exception object is passed as the first argument so Serilog
            // captures it as a structured ExceptionDetail property — not just a
            // formatted string — enabling downstream log aggregators (e.g. Seq,
            // Elastic) to index type, message, and frames individually.
            _logger.LogError(
                ex,
                "Unhandled exception | Method={Method} | Path={Path} | " +
                "Query={Query} | RemoteIp={RemoteIp} | UserId={UserId} | " +
                "ExceptionType={ExceptionType} | ExceptionMessage={ExceptionMessage}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "(none)",
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
                ex.GetType().FullName,
                ex.Message);

            var correlationId = context.Response.Headers["X-Correlation-Id"].ToString();
            await WriteJsonResponseAsync(
                context,
                (int)HttpStatusCode.InternalServerError,
                ApiResponse<object>.Fail(
                    $"An unexpected error occurred. Reference ID: {correlationId}"));
        }
    }

    // ── Helper ─────────────────────────────────────────────────────────────────

    private static async Task WriteJsonResponseAsync<T>(
        HttpContext context, int statusCode, ApiResponse<T> body)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, _jsonOptions));
    }
}