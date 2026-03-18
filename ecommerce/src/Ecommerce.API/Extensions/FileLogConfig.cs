namespace Ecommerce.API.Extensions;

/// <summary>
/// Per-sink file configuration read from appsettings Logging:Trace / Logging:App / Logging:Error sections.
/// Shared by both <see cref="LoggingExtensions"/> and <see cref="LokiLoggingExtensions"/>.
/// </summary>
public class FileLogConfig
{
    public string Path { get; set; } = "logs/default-.log";
    public int RetainedFileCountLimit { get; set; } = 30;
    public string OutputTemplate { get; set; } =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] " +
        "[T:{TraceId}] [U:{UserId}] " +
        "{Message:lj}{NewLine}{Exception}";
}