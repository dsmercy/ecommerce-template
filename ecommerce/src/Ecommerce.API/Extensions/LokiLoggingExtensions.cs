using Ecommerce.API.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

namespace Ecommerce.API.Extensions;

public static class LokiLoggingExtensions
{
    public static IHostBuilder ConfigureSerilogWithLoki(
        this IHostBuilder builder,
        IConfiguration configuration)
    {
        return builder.UseSerilog((ctx, services, logConfig) =>
        {
            var traceConfig = configuration.GetSection("Logging:Trace").Get<FileLogConfig>()
                ?? new FileLogConfig { Path = "logs/trace-.log" };
            var appConfig = configuration.GetSection("Logging:App").Get<FileLogConfig>()
                ?? new FileLogConfig { Path = "logs/app-.log" };
            var errorConfig = configuration.GetSection("Logging:Error").Get<FileLogConfig>()
                ?? new FileLogConfig { Path = "logs/errors-.log" };

            var useLoki = string.Equals(configuration["UseLokiLogs"], "true", StringComparison.OrdinalIgnoreCase);
            var lokiUrl = configuration["Loki:Url"] ?? "http://localhost:3100";
            var appLabel = configuration["Loki:AppName"] ?? "ecommerce-api";
            var env = ctx.HostingEnvironment.EnvironmentName.ToLowerInvariant();

            logConfig
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.StaticFiles", LogEventLevel.Warning)

                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.With<ActivityEnricher>()
                .Enrich.WithProperty("app", appLabel)   // default — LogsController overrides per frontend event
                .Enrich.WithProperty("env", env)

                .ReadFrom.Services(services);

            if (useLoki)
            {
                // ── LOKI MODE ──────────────────────────────────────────────────
                // Loki is the only destination. No file sinks.
                // If Loki is unreachable at startup we throw — fail fast so the
                // developer knows immediately rather than silently losing logs.
                EnsureLokiReachable(lokiUrl);   // throws if down

                logConfig
                    .WriteTo.GrafanaLoki(
                        uri: lokiUrl,
                        propertiesAsLabels: new[] { "app", "env", "level" },
                        restrictedToMinimumLevel: LogEventLevel.Information,
                        batchPostingLimit: 1000,
                        period: TimeSpan.FromSeconds(2))

                    // Console kept in dev so you still see output locally
                    .WriteTo.Conditional(
                        _ => ctx.HostingEnvironment.IsDevelopment(),
                        wt => wt.Console(
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] [{app}] {Message:lj}{NewLine}{Exception}",
                            restrictedToMinimumLevel: LogEventLevel.Information));

                Log.Information("Logging mode: Loki only @ {LokiUrl} | app={AppLabel} env={Env}",
                    lokiUrl, appLabel, env);
            }
            else
            {
                // ── FILE MODE ──────────────────────────────────────────────────
                // UseLokiLogs=false — write everything to files.
                // Both API and UI logs land here (UI via LogsController → Serilog).
                logConfig
                    // Trace sink: Verbose/Debug
                    .WriteTo.Logger(lc => lc
                        .Filter.ByIncludingOnly(e => e.Level <= LogEventLevel.Debug)
                        .WriteTo.File(
                            path: traceConfig.Path,
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: traceConfig.RetainedFileCountLimit,
                            outputTemplate: traceConfig.OutputTemplate))

                    // App sink: Information/Warning
                    .WriteTo.Logger(lc => lc
                        .Filter.ByIncludingOnly(e =>
                            e.Level >= LogEventLevel.Information &&
                            e.Level < LogEventLevel.Error)
                        .WriteTo.File(
                            path: appConfig.Path,
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: appConfig.RetainedFileCountLimit,
                            outputTemplate: appConfig.OutputTemplate))

                    // Error sink: Error/Fatal
                    .WriteTo.Logger(lc => lc
                        .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
                        .WriteTo.File(
                            path: errorConfig.Path,
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: errorConfig.RetainedFileCountLimit,
                            outputTemplate: errorConfig.OutputTemplate))

                    .WriteTo.Conditional(
                        _ => ctx.HostingEnvironment.IsDevelopment(),
                        wt => wt.Console(
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] [{app}] {Message:lj}{NewLine}{Exception}",
                            restrictedToMinimumLevel: LogEventLevel.Information));

                Log.Information("Logging mode: files only (UseLokiLogs=false) — trace={Trace} app={App} errors={Errors}",
                    traceConfig.Path, appConfig.Path, errorConfig.Path);
            }
        });
    }

    /// <summary>
    /// Verifies Loki is reachable before the app starts.
    /// Throws <see cref="InvalidOperationException"/> if the /ready endpoint
    /// does not return 2xx within 3 seconds.
    ///
    /// This is intentional fail-fast behaviour: when UseLokiLogs=true,
    /// Loki is the ONLY log destination. A silent failure would mean the
    /// entire application runs with no logging at all — worse than crashing.
    /// </summary>
    private static void EnsureLokiReachable(string lokiUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = http.GetAsync($"{lokiUrl}/ready").GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Loki connectivity check: OK ({LokiUrl}/ready)", lokiUrl);
                return;
            }

            throw new InvalidOperationException(
                $"Loki is not ready. {lokiUrl}/ready returned HTTP {(int)response.StatusCode}. " +
                $"Fix Loki or set UseLokiLogs=false to fall back to file logging.");
        }
        catch (InvalidOperationException)
        {
            throw;  // re-throw our own descriptive exception as-is
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Loki is unreachable at {lokiUrl}/ready — {ex.Message}. " +
                $"Ensure 'docker compose up -d loki' is running, " +
                $"or set UseLokiLogs=false to fall back to file logging.", ex);
        }
    }
}