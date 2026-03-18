using System.Diagnostics;
using Ecommerce.API.Extensions;
using Ecommerce.API.Logging;
using Ecommerce.API.Middleware;
using Ecommerce.Application;
using Ecommerce.Infrastructure;
using Serilog;
using Serilog.Core;

// ── Bootstrap Serilog before the host so startup errors are captured ──────────
// A minimal bootstrap logger writes to console only; the full pipeline is
// configured in ConfigureSerilog() once appsettings are available.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Distributed tracing — .NET ActivitySource ─────────────────────────────
    // Creates an application-level ActivitySource for the API layer.
    // Any middleware or service can resolve IActivitySource (or use the static field)
    // to start custom spans. The W3C TraceId produced here is what ActivityEnricher
    // reads and injects into every Serilog log entry.
    //
    // No external agent (Jaeger, Zipkin) is required — the trace IDs are emitted
    // directly into structured logs. To export to a tracing backend, add
    // OpenTelemetry.Extensions.Hosting and configure an exporter here.
    //
    // ASP.NET Core's built-in diagnostics (DiagnosticSource) automatically creates
    // Activities for every HTTP request when a listener is attached; registering
    // the ActivitySource here lets us create custom child spans inside handlers.
    var activitySource = new ActivitySource("Ecommerce.API");
    builder.Services.AddSingleton(activitySource);

    // Ensure .NET's default W3C trace propagation is used for distributed tracing
    Activity.DefaultIdFormat = ActivityIdFormat.W3C;
    Activity.ForceDefaultIdFormat = true;

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.ConfigureSerilogWithLoki(builder.Configuration);

    Log.Information("UseLokiLogs flag value: {Value}",
    builder.Configuration["UseLokiLogs"]);

    Log.Information("Starting Ecommerce API");

    // ── Services ──────────────────────────────────────────────────────────────
    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddJwtAuthentication(builder.Configuration)
        .AddSwagger();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // IHttpContextAccessor is required by RequestContextEnricher
    builder.Services.AddHttpContextAccessor();

    // Register RequestContextEnricher as a Serilog ILogEventEnricher so that
    // UseSerilog(...).ReadFrom.Services(services) can resolve it via DI.
    // This is what makes UserId + CorrelationId appear on every log entry
    // without manually pushing them in each handler.
    // Must be Singleton — ReadFrom.Services resolves from the root provider
    // during host build. Scoped registrations throw at startup.
    // IHttpContextAccessor is also singleton, so this is safe.
    builder.Services.AddSingleton<ILogEventEnricher, RequestContextEnricher>();

    builder.Services.AddCors(opts =>
        opts.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    builder.Services.AddHealthChecks();

    // ── Build App ─────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Middleware Pipeline ───────────────────────────────────────────────────
    // Order matters — earlier middleware wraps later ones in the try/catch sense.
    //
    // 1. CorrelationIdMiddleware   — FIRST: assigns + pushes CorrelationId into
    //                                LogContext so every log below has it.
    // 2. ExceptionHandlingMiddleware — SECOND: catches exceptions from all layers
    //                                below; by this point CorrelationId is in scope.
    // 3. RequestBodyLoggingMiddleware — THIRD: buffers body before MVC reads it;
    //                                must run before model binding.
    // 4. RequestLoggingMiddleware  — FOURTH: logs inbound/outbound summary.
    // 5. Framework middleware (Swagger, HTTPS, CORS, Auth, Routing…)

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<RequestBodyLoggingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce API v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}