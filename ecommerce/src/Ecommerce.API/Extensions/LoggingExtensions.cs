using Serilog;
using Serilog.Events;

namespace Ecommerce.API.Extensions;

public static class LoggingExtensions
{
    public static IHostBuilder ConfigureSerilog(this IHostBuilder builder, IConfiguration configuration)
    {
        var traceConfig = configuration.GetSection("Logging:Trace").Get<FileLogConfig>();
        var errorConfig = configuration.GetSection("Logging:Error").Get<FileLogConfig>();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()

            // ── Trace log (Verbose/Debug only) ─────────────────────────────────
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Level <= LogEventLevel.Debug)
                .WriteTo.File(
                    path: traceConfig.Path,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: traceConfig.RetainedFileCountLimit,
                    outputTemplate: traceConfig.OutputTemplate))

            // ── Error log (Error/Fatal only) ───────────────────────────────────
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
                .WriteTo.File(
                    path: errorConfig.Path,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: errorConfig.RetainedFileCountLimit,
                    outputTemplate: errorConfig.OutputTemplate))

            .CreateLogger();

        return builder.UseSerilog();
    }
}

public class FileLogConfig
{
    public string Path { get; set; } = "logs/default-.log";
    public int RetainedFileCountLimit { get; set; } = 30;
    public string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
}