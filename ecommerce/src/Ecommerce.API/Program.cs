using Ecommerce.API.Extensions;
using Ecommerce.API.Middleware;
using Ecommerce.Application;
using Ecommerce.Infrastructure;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Configure Serilog from appsettings ─────────────────────────────────────
    builder.Host.ConfigureSerilog(builder.Configuration);

    Log.Information("Starting Ecommerce API");

    // ── Services ───────────────────────────────────────────────────────────────
    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddJwtAuthentication(builder.Configuration)
        .AddSwagger();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddCors(opts =>
        opts.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    builder.Services.AddHealthChecks();

    // ── Build App ──────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Middleware Pipeline ────────────────────────────────────────────────────
    app.UseMiddleware<ExceptionHandlingMiddleware>();
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