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
        var traceConfig = configuration.GetSection("Logging:Trace").Get<FileLogConfig>()
            ?? new FileLogConfig { Path = "logs/trace-.log" };
        var appConfig = configuration.GetSection("Logging:App").Get<FileLogConfig>()
            ?? new FileLogConfig { Path = "logs/app-.log" };
        var errorConfig = configuration.GetSection("Logging:Error").Get<FileLogConfig>()
            ?? new FileLogConfig { Path = "logs/errors-.log" };

        // ── READ THE FLAG ──────────────────────────────────────────────────────
        // Supports both environment variable and appsettings key.
        // Environment variable (docker-compose / launchSettings) takes precedence.
        var useLoki = string.Equals(
            configuration["UseLokiLogs"], "true",
            StringComparison.OrdinalIgnoreCase);

        var lokiUrl = configuration["Loki:Url"] ?? "http://localhost:3100";
        var appLabel = configuration["Loki:AppName"] ?? "ecommerce-api";

        return builder.UseSerilog((ctx, services, logConfig) =>
        {
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
                .Enrich.WithProperty("app", appLabel)
                .Enrich.WithProperty("env", env)

                .ReadFrom.Services(services)

                // ── Trace sink ─────────────────────────────────────────────────
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level <= LogEventLevel.Debug)
                    .WriteTo.File(
                        path: traceConfig.Path,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: traceConfig.RetainedFileCountLimit,
                        outputTemplate: traceConfig.OutputTemplate))

                // ── App sink ───────────────────────────────────────────────────
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e =>
                        e.Level >= LogEventLevel.Information &&
                        e.Level < LogEventLevel.Error)
                    .WriteTo.File(
                        path: appConfig.Path,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: appConfig.RetainedFileCountLimit,
                        outputTemplate: appConfig.OutputTemplate))

                // ── Error sink ─────────────────────────────────────────────────
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
                    .WriteTo.File(
                        path: errorConfig.Path,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: errorConfig.RetainedFileCountLimit,
                        outputTemplate: errorConfig.OutputTemplate))

                // ── Loki sink: CONDITIONAL on UseLokiLogs=true ─────────────────
                .WriteTo.Conditional(
                    _ => useLoki,
                    wt => wt.GrafanaLoki(
                        uri: lokiUrl,
                        labels: new[]
                        {
                            new LokiLabel { Key = "app", Value = appLabel },
                            new LokiLabel { Key = "env", Value = env }
                        },
                        propertiesAsLabels: new[] { "level" },
                        restrictedToMinimumLevel: LogEventLevel.Information,
                        batchPostingLimit: 1000,
                        period: TimeSpan.FromSeconds(2)))

                // ── Console (dev only) ─────────────────────────────────────────
                .WriteTo.Conditional(
                    _ => ctx.HostingEnvironment.IsDevelopment(),
                    wt => wt.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}",
                        restrictedToMinimumLevel: LogEventLevel.Information));

            // Startup confirmation — visible immediately in the bootstrap console
            if (useLoki)
                Log.Information("Logging mode: files + Loki @ {LokiUrl}", lokiUrl);
            else
                Log.Information("Logging mode: files only (UseLokiLogs=false)");
        });
    }
}