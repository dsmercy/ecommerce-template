using Ecommerce.API.Logging;
using Serilog;
using Serilog.Events;

namespace Ecommerce.API.Extensions;

public static class LoggingExtensions
{
    /// <summary>
    /// Configures Serilog with three file sinks and two structural enrichers:
    ///
    ///   Sinks
    ///   ─────
    ///   trace-*.log   — Verbose/Debug only.  Full request detail, bodies, headers.
    ///   app-*.log     — Information/Warning.  One-line request summaries, business events.
    ///   errors-*.log  — Error/Fatal only.     Exceptions with full stack trace + context.
    ///
    ///   Global enrichers
    ///   ────────────────
    ///   ActivityEnricher        — injects W3C TraceId / SpanId / ParentSpanId from
    ///                             Activity.Current into every log entry.
    ///   RequestContextEnricher  — registered via DI (IHttpContextAccessor) so it can
    ///                             read the per-request UserId and CorrelationId that
    ///                             CorrelationIdMiddleware has already resolved.
    ///
    /// The DI-based enricher (RequestContextEnricher) is wired in Program.cs via
    ///   .UseSerilog((ctx, services, cfg) => cfg.ReadFrom.Services(services)...)
    /// which means this method only needs to register the static enrichers.
    /// </summary>
    public static IHostBuilder ConfigureSerilog(this IHostBuilder builder, IConfiguration configuration)
    {
        var traceConfig = configuration.GetSection("Logging:Trace").Get<FileLogConfig>()
            ?? new FileLogConfig { Path = "logs/trace-.log" };
        var appConfig = configuration.GetSection("Logging:App").Get<FileLogConfig>()
            ?? new FileLogConfig { Path = "logs/app-.log" };
        var errorConfig = configuration.GetSection("Logging:Error").Get<FileLogConfig>()
            ?? new FileLogConfig { Path = "logs/errors-.log" };

        return builder.UseSerilog((ctx, services, logConfig) =>
        {
            logConfig
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.StaticFiles", LogEventLevel.Warning)

                // ── Static enrichers (no DI required) ─────────────────────
                .Enrich.FromLogContext()          // CorrelationId pushed by CorrelationIdMiddleware
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.With<ActivityEnricher>()  // W3C TraceId / SpanId

                // ── DI-based enrichers (IHttpContextAccessor → UserId, etc.) ─
                // Serilog.Extensions.Hosting resolves these from the DI container.
                .ReadFrom.Services(services)

                // ── Trace sink: Verbose/Debug only ─────────────────────────
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level <= LogEventLevel.Debug)
                    .WriteTo.File(
                        path: traceConfig.Path,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: traceConfig.RetainedFileCountLimit,
                        outputTemplate: traceConfig.OutputTemplate))

                // ── App sink: Information / Warning ────────────────────────
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e =>
                        e.Level >= LogEventLevel.Information &&
                        e.Level < LogEventLevel.Error)
                    .WriteTo.File(
                        path: appConfig.Path,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: appConfig.RetainedFileCountLimit,
                        outputTemplate: appConfig.OutputTemplate))

                // ── Error sink: Error/Fatal only ───────────────────────────
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
                    .WriteTo.File(
                        path: errorConfig.Path,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: errorConfig.RetainedFileCountLimit,
                        outputTemplate: errorConfig.OutputTemplate))

                // ── Console (dev only) ─────────────────────────────────────
                .WriteTo.Conditional(
                    _ => ctx.HostingEnvironment.IsDevelopment(),
                    wt => wt.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}",
                        restrictedToMinimumLevel: LogEventLevel.Information));
        });
    }
}

/// <summary>Per-sink configuration read from appsettings Logging:* sections.</summary>
public class FileLogConfig
{
    public string Path { get; set; } = "logs/default-.log";
    public int RetainedFileCountLimit { get; set; } = 30;
    public string OutputTemplate { get; set; } =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] " +
        "[T:{TraceId}] [U:{UserId}] " +
        "{Message:lj}{NewLine}{Exception}";
}