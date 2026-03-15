using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Ecommerce.API.Logging;

/// <summary>
/// Serilog <see cref="ILogEventEnricher"/> that reads the current
/// <see cref="Activity"/> (W3C distributed tracing) and injects its identifiers
/// into every structured log entry.
///
/// Properties added when an active Activity is present:
///   TraceId       — W3C 128-bit hex trace identifier (32 chars)
///   SpanId        — W3C 64-bit hex span identifier (16 chars)
///   ParentSpanId  — 16-char parent span id, or "(root)" for the root span
///
/// This wires Serilog's structured log output to any W3C-compatible distributed
/// tracing system (Jaeger, Zipkin, AWS X-Ray, Azure Application Insights, etc.)
/// because the TraceId in logs matches the TraceId visible in the tracer UI.
///
/// When no Activity is active (background jobs, startup code, etc.) the properties
/// are omitted entirely to avoid noise.
/// </summary>
public sealed class ActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null) return;

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));

        var parentSpanId = activity.ParentSpanId == default
            ? "(root)"
            : activity.ParentSpanId.ToString();

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("ParentSpanId", parentSpanId));
    }
}