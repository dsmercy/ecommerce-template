using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace Ecommerce.API.Middleware;

/// <summary>
/// Logs HTTP requests/responses for API endpoints only (excluding swagger, health checks, etc).
/// 
/// Information  — one-line summary written to app-*.log and console.
/// Trace        — detailed headers, body size, and query string written
///                exclusively to trace-*.log (Serilog Verbose sink).
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    // Paths to exclude from logging (framework, health, swagger)
    private static readonly HashSet<string> _excludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/swagger",
        "/health",
        "/_framework",
        "/_vs",
        "/favicon.ico"
    };

    // Headers that are safe to log in plain text.
    private static readonly HashSet<string> _safeHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept", "Accept-Encoding", "Accept-Language",
        "Cache-Control", "Connection", "Content-Type", "Content-Length",
        "Host", "Origin", "Referer", "User-Agent",
        "X-Forwarded-For", "X-Real-IP", "X-Request-ID"
    };

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for excluded paths (swagger, health, framework, etc)
        if (ShouldSkipLogging(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                 ?? Guid.NewGuid().ToString()[..8];
        context.Response.Headers.Append("X-Correlation-Id", correlationId);

        using var logScope = LogContext.PushProperty("CorrelationId", correlationId);

        // ── Information: one-line inbound entry ───────────────────────────────
        _logger.LogInformation(
            "[{CorrelationId}] → {Method} {Path}{Query}",
            correlationId,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);

        // ── Trace: full request detail ─────────────────────────────────────────
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            LogRequestTrace(context, correlationId);
        }

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();

            var statusCode = context.Response.StatusCode;
            var elapsedMs = sw.ElapsedMilliseconds;

            // ── Information / Warning / Error: one-line outbound entry ─────────
            var summaryLevel = statusCode >= 500
                ? LogLevel.Error
                : statusCode >= 400
                    ? LogLevel.Warning
                    : LogLevel.Information;

            _logger.Log(
                summaryLevel,
                "[{CorrelationId}] ← {StatusCode} {Method} {Path} [{ElapsedMs}ms]",
                correlationId,
                statusCode,
                context.Request.Method,
                context.Request.Path,
                elapsedMs);

            // ── Trace: full response detail ────────────────────────────────────
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogResponseTrace(context, correlationId, elapsedMs);
            }
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static bool ShouldSkipLogging(PathString path)
    {
        var pathValue = path.Value ?? string.Empty;
        return _excludedPaths.Any(excludedPath => pathValue.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase));
    }

    private void LogRequestTrace(HttpContext context, string correlationId)
    {
        var req = context.Request;
        var headers = FilterHeaders(req.Headers);

        _logger.LogTrace(
            "[{CorrelationId}] REQUEST DETAIL | " +
            "Method={Method} | Path={Path} | QueryString={Query} | " +
            "ContentType={ContentType} | ContentLength={ContentLength} | " +
            "Protocol={Protocol} | IsHttps={IsHttps} | " +
            "RemoteIp={RemoteIp} | SafeHeaders={Headers}",
            correlationId,
            req.Method,
            req.Path,
            req.QueryString.HasValue ? req.QueryString.Value : "(none)",
            req.ContentType ?? "(none)",
            req.ContentLength.HasValue ? req.ContentLength.Value.ToString() : "(none)",
            req.Protocol,
            req.IsHttps,
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            headers);
    }

    private void LogResponseTrace(HttpContext context, string correlationId, long elapsedMs)
    {
        var res = context.Response;

        _logger.LogTrace(
            "[{CorrelationId}] RESPONSE DETAIL | " +
            "StatusCode={StatusCode} | ContentType={ContentType} | " +
            "ContentLength={ContentLength} | ElapsedMs={ElapsedMs} | " +
            "UserId={UserId}",
            correlationId,
            res.StatusCode,
            res.ContentType ?? "(none)",
            res.ContentLength.HasValue ? res.ContentLength.Value.ToString() : "(chunked/unknown)",
            elapsedMs,
            context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous");
    }

    /// <summary>
    /// Returns only safe, non-sensitive headers as a comma-separated string.
    /// Authorization, Cookie, and other sensitive headers are intentionally omitted.
    /// </summary>
    private static string FilterHeaders(IHeaderDictionary headers)
    {
        var parts = headers
            .Where(h => _safeHeaders.Contains(h.Key))
            .Select(h => $"{h.Key}={h.Value}");

        return string.Join(", ", parts);
    }
}