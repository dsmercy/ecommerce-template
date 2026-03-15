using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Ecommerce.Application.Common.Models;
using FluentValidation;

namespace Ecommerce.API.Middleware;

/// <summary>
/// Catches all unhandled exceptions, translates them to RFC-7807-style JSON
/// responses, and writes structured log entries enriched with:
///
///   • CorrelationId   — from <see cref="CorrelationIdMiddleware"/> (HttpContext.Items)
///   • TraceId/SpanId  — from <see cref="Activity.Current"/> (W3C distributed trace)
///   • UserId          — from the JWT NameIdentifier claim, or "anonymous"
///   • RemoteIp        — caller's IP address
///   • ExceptionType   — full CLR type name (e.g. "System.InvalidOperationException")
///
/// Log levels:
///   Warning  — validation errors, 401s, 404s (expected business failures)
///   Error    — all other unhandled exceptions (alerts warranted)
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
            var ctx = BuildErrorContext(context);
            var errors = ex.Errors.Select(e => e.ErrorMessage).ToList();

            _logger.LogWarning(
                ex,
                "Validation failed | " +
                "CorrelationId={CorrelationId} | TraceId={TraceId} | SpanId={SpanId} | " +
                "Path={Path} | Method={Method} | RemoteIp={RemoteIp} | UserId={UserId} | " +
                "ErrorCount={ErrorCount} | Errors={Errors}",
                ctx.CorrelationId, ctx.TraceId, ctx.SpanId,
                ctx.Path, ctx.Method, ctx.RemoteIp, ctx.UserId,
                errors.Count, string.Join("; ", errors));

            await WriteJsonResponseAsync(
                context,
                (int)HttpStatusCode.BadRequest,
                ApiResponse<object>.Fail("Validation failed.", errors));
        }
        catch (UnauthorizedAccessException ex)
        {
            var ctx = BuildErrorContext(context);

            _logger.LogWarning(
                ex,
                "Unauthorized access | " +
                "CorrelationId={CorrelationId} | TraceId={TraceId} | SpanId={SpanId} | " +
                "Path={Path} | Method={Method} | RemoteIp={RemoteIp} | UserId={UserId} | " +
                "Message={Message}",
                ctx.CorrelationId, ctx.TraceId, ctx.SpanId,
                ctx.Path, ctx.Method, ctx.RemoteIp, ctx.UserId,
                ex.Message);

            await WriteJsonResponseAsync(
                context,
                (int)HttpStatusCode.Unauthorized,
                ApiResponse<object>.Fail("Unauthorized."));
        }
        catch (KeyNotFoundException ex)
        {
            var ctx = BuildErrorContext(context);

            _logger.LogWarning(
                ex,
                "Resource not found | " +
                "CorrelationId={CorrelationId} | TraceId={TraceId} | SpanId={SpanId} | " +
                "Path={Path} | Method={Method} | RemoteIp={RemoteIp} | UserId={UserId} | " +
                "Message={Message}",
                ctx.CorrelationId, ctx.TraceId, ctx.SpanId,
                ctx.Path, ctx.Method, ctx.RemoteIp, ctx.UserId,
                ex.Message);

            await WriteJsonResponseAsync(
                context,
                (int)HttpStatusCode.NotFound,
                ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            var ctx = BuildErrorContext(context);

            // Full structured error entry.
            // Passing the exception as the first argument lets Serilog capture it as a
            // structured ExceptionDetail — not a flat string — enabling downstream
            // aggregators (Seq, Elastic, Grafana Loki) to index type/message/frames.
            _logger.LogError(
                ex,
                "Unhandled exception | " +
                "CorrelationId={CorrelationId} | TraceId={TraceId} | SpanId={SpanId} | ParentSpanId={ParentSpanId} | " +
                "Method={Method} | Path={Path} | Query={Query} | " +
                "RemoteIp={RemoteIp} | UserId={UserId} | " +
                "ExceptionType={ExceptionType} | ExceptionMessage={ExceptionMessage}",
                ctx.CorrelationId, ctx.TraceId, ctx.SpanId, ctx.ParentSpanId,
                ctx.Method, ctx.Path, ctx.Query,
                ctx.RemoteIp, ctx.UserId,
                ex.GetType().FullName, ex.Message);

            await WriteJsonResponseAsync(
                context,
                (int)HttpStatusCode.InternalServerError,
                ApiResponse<object>.Fail(
                    $"An unexpected error occurred. Reference ID: {ctx.CorrelationId}"));
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static ErrorContext BuildErrorContext(HttpContext context)
    {
        var activity = Activity.Current;

        return new ErrorContext
        {
            CorrelationId = context.Items.TryGetValue("CorrelationId", out var cid) && cid is string s
                ? s
                : context.Response.Headers["X-Correlation-Id"].FirstOrDefault() ?? "(none)",

            TraceId = activity?.TraceId.ToString() ?? "(none)",
            SpanId = activity?.SpanId.ToString() ?? "(none)",
            ParentSpanId = activity?.ParentSpanId.ToString() ?? "(none)",

            Path = context.Request.Path,
            Method = context.Request.Method,
            Query = context.Request.QueryString.HasValue
                          ? context.Request.QueryString.Value
                          : "(none)",

            RemoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",

            UserId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? context.User?.FindFirstValue("sub")
                  ?? "anonymous"
        };
    }

    private static async Task WriteJsonResponseAsync<T>(
        HttpContext context, int statusCode, ApiResponse<T> body)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
        }
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, _jsonOptions));
    }

    // ── Private record ───────────────────────────────────────────────────────

    private sealed record ErrorContext
    {
        public string CorrelationId { get; init; } = string.Empty;
        public string TraceId { get; init; } = string.Empty;
        public string SpanId { get; init; } = string.Empty;
        public string ParentSpanId { get; init; } = string.Empty;
        public string Path { get; init; } = string.Empty;
        public string Method { get; init; } = string.Empty;
        public string Query { get; init; } = string.Empty;
        public string RemoteIp { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;
    }
}