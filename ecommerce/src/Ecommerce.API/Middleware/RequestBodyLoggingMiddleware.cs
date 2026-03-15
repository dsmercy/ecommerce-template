using System.Text;

namespace Ecommerce.API.Middleware;

/// <summary>
/// Reads and logs the HTTP request body at <c>Verbose/Trace</c> level.
///
/// Design constraints:
///   • Only fires when the logger has Trace/Verbose enabled (checked before buffering).
///   • Only processes <c>application/json</c> and <c>application/x-www-form-urlencoded</c>.
///     Binary payloads (multipart/form-data, image/*, etc.) are skipped entirely
///     so image uploads are never buffered.
///   • Enforces a configurable <see cref="MaxBodyBytes"/> cap (default 32 KB).
///     Bodies larger than the cap are logged as a truncation notice, not read.
///   • Replaces <see cref="HttpRequest.Body"/> with a buffered stream so downstream
///     middleware/model binders can still read the body normally.
///   • Request bodies are logged WITHOUT pretty-printing to avoid leaking sensitive
///     field values through formatting artefacts — the raw JSON string is captured.
///
/// Security note:
///   Sensitive fields (passwords, tokens, card numbers) are NOT masked here because
///   masking is error-prone when field names vary across endpoints. Instead, ensure
///   this middleware's Trace log sink is stored securely and excluded from
///   production log aggregators unless explicitly needed for debugging.
/// </summary>
public class RequestBodyLoggingMiddleware
{
    /// <summary>Maximum body size in bytes that will be read and logged.</summary>
    public const int MaxBodyBytes = 32 * 1024; // 32 KB

    private static readonly HashSet<string> _loggableContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
        "application/x-www-form-urlencoded",
        "text/plain"
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestBodyLoggingMiddleware> _logger;

    public RequestBodyLoggingMiddleware(RequestDelegate next, ILogger<RequestBodyLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only log when the Trace sink is actually active — avoid buffering overhead
        // on deployments where Trace logging is disabled.
        if (!_logger.IsEnabled(LogLevel.Trace))
        {
            await _next(context);
            return;
        }

        var request = context.Request;

        // Skip non-body methods, multipart/binary content types, and missing content
        if (!IsLoggable(request))
        {
            await _next(context);
            return;
        }

        var correlationId = context.Items.TryGetValue("CorrelationId", out var c) && c is string cid
            ? cid
            : "(unknown)";

        // Guard: skip if Content-Length is already known to exceed the cap
        if (request.ContentLength.HasValue && request.ContentLength > MaxBodyBytes)
        {
            _logger.LogTrace(
                "[{CorrelationId}] REQUEST BODY SKIPPED | ContentLength={ContentLength} exceeds cap of {Cap} bytes",
                correlationId, request.ContentLength, MaxBodyBytes);

            await _next(context);
            return;
        }

        // Buffer the request body so downstream code can still read it
        request.EnableBuffering();

        try
        {
            using var reader = new StreamReader(
                request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: MaxBodyBytes,
                leaveOpen: true); // keep the stream open for model binding

            // Read up to MaxBodyBytes + 1 to detect truncation
            var buffer = new char[MaxBodyBytes + 1];
            var charsRead = await reader.ReadAsync(buffer, 0, buffer.Length);

            string bodyText;
            bool truncated = false;

            if (charsRead > MaxBodyBytes)
            {
                bodyText = new string(buffer, 0, MaxBodyBytes);
                truncated = true;
            }
            else
            {
                bodyText = new string(buffer, 0, charsRead);
            }

            if (truncated)
            {
                _logger.LogTrace(
                    "[{CorrelationId}] REQUEST BODY (TRUNCATED at {Cap} bytes) | " +
                    "ContentType={ContentType} | Body={Body}",
                    correlationId, MaxBodyBytes, request.ContentType, bodyText);
            }
            else
            {
                _logger.LogTrace(
                    "[{CorrelationId}] REQUEST BODY | ContentType={ContentType} | Body={Body}",
                    correlationId, request.ContentType, bodyText);
            }
        }
        catch (Exception ex)
        {
            // Never fail a request due to logging infrastructure
            _logger.LogWarning(ex,
                "[{CorrelationId}] REQUEST BODY logging failed — continuing without body log",
                correlationId);
        }
        finally
        {
            // Rewind so model binding reads from the start
            if (request.Body.CanSeek)
                request.Body.Seek(0, SeekOrigin.Begin);
        }

        await _next(context);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static bool IsLoggable(HttpRequest request)
    {
        // No body expected for these verbs
        if (HttpMethods.IsGet(request.Method)
            || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsDelete(request.Method)
            || HttpMethods.IsOptions(request.Method))
            return false;

        var contentType = request.ContentType;
        if (string.IsNullOrEmpty(contentType)) return false;

        // Strip parameters like "; charset=utf-8" before matching
        var baseType = contentType.Split(';')[0].Trim();
        return _loggableContentTypes.Contains(baseType);
    }
}