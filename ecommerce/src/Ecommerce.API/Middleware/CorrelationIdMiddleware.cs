using Serilog.Context;

namespace Ecommerce.API.Middleware;

/// <summary>
/// Assigns a correlation ID to every inbound request and pushes it into the
/// Serilog LogContext so every log entry written during the request carries it.
///
/// Resolution order (first non-empty value wins):
///   1. X-Correlation-Id request header  — lets callers propagate an existing ID
///   2. traceparent W3C header trace-id   — extracted from "traceparent: 00-{traceId}-{spanId}-{flags}"
///   3. New short GUID                    — generated when neither header is present
///
/// The resolved value is:
///   • Written to X-Correlation-Id response header so callers can correlate logs.
///   • Stored in HttpContext.Items["CorrelationId"] for downstream middleware.
///   • Pushed into Serilog's LogContext as the CorrelationId property.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string TraceParentHeader = "traceparent";
    private const string ContextItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request);

        // Store on context so other middleware can read it without re-parsing headers
        context.Items[ContextItemKey] = correlationId;

        // Echo back on the response so API consumers can correlate
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Push into Serilog LogContext for the lifetime of this request
        using var _ = LogContext.PushProperty(ContextItemKey, correlationId);

        await _next(context);
    }

    // ── Resolution helpers ──────────────────────────────────────────────────

    private static string ResolveCorrelationId(HttpRequest request)
    {
        // 1. Explicit X-Correlation-Id header
        var explicitId = request.Headers[CorrelationIdHeader].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(explicitId))
            return explicitId.Trim();

        // 2. W3C traceparent — format: "00-{32hex traceId}-{16hex spanId}-{2hex flags}"
        var traceParent = request.Headers[TraceParentHeader].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(traceParent))
        {
            var parts = traceParent.Split('-');
            if (parts.Length >= 2 && parts[1].Length == 32)
                return parts[1]; // use W3C trace-id as correlation ID
        }

        // 3. Generate a new short ID (8 hex chars — readable in logs without wrapping)
        return Guid.NewGuid().ToString("N")[..8];
    }
}