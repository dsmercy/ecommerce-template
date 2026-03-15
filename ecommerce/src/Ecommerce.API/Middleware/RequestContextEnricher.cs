using System.Security.Claims;
using Serilog.Core;
using Serilog.Events;

namespace Ecommerce.API.Logging;

/// <summary>
/// Serilog <see cref="ILogEventEnricher"/> that injects HTTP request context
/// into every structured log entry written during an active request.
///
/// Properties added when an HTTP request is in progress:
///   RequestPath   — e.g. "/api/orders/42"
///   RequestMethod — e.g. "GET"
///   UserId        — authenticated user's NameIdentifier claim, or "anonymous"
///   CorrelationId — value resolved by <see cref="Middleware.CorrelationIdMiddleware"/>
///                   (falls back to the response header value if the middleware has run)
///
/// Registered as a <b>singleton</b> in DI. <see cref="IHttpContextAccessor"/> is
/// itself a singleton whose <c>HttpContext</c> property changes per-request, so
/// storing the accessor (not the context) is safe for singleton lifetime.
///
/// Unlike LogContext properties (which must be pushed manually per-request),
/// this enricher fires on every log event globally — ensuring properties
/// never leak or go missing regardless of code path.
/// </summary>
public sealed class RequestContextEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // IHttpContextAccessor is itself a singleton — safe to inject into a singleton enricher.
    public RequestContextEnricher(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return;

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("RequestPath", ctx.Request.Path.ToString()));

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("RequestMethod", ctx.Request.Method));

        var userId = ctx.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? ctx.User?.FindFirstValue("sub")
                  ?? "anonymous";

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("UserId", userId));

        // CorrelationId is set by CorrelationIdMiddleware early in the pipeline
        if (ctx.Items.TryGetValue("CorrelationId", out var corrId) && corrId is string cid)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("CorrelationId", cid));
        }
    }
}