using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Cache;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.QueryServices;
using Ecommerce.Infrastructure.Repositories;
using Ecommerce.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Ecommerce.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // ── EF Core / MySQL ────────────────────────────────────────────────────
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(opts =>
            opts.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                mysql => mysql.EnableRetryOnFailure(3)));

        // ── Repositories ───────────────────────────────────────────────────────
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── View-backed Query Services ─────────────────────────────────────────
        // These implement interfaces defined in Application layer.
        // Application code depends on the interfaces only — never AppDbContext directly.
        // This is the Dependency Inversion Principle applied to break the cycle:
        //
        //   Application defines:  IProductQueryService, IInventoryQueryService, IOrderQueryService
        //   Infrastructure impls: ProductQueryService, InventoryQueryService, OrderQueryService
        //
        //   Dependency flow: Domain ◄── Application ◄── Infrastructure ◄── API
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<IInventoryQueryService, InventoryQueryService>();
        services.AddScoped<IInventoryWriteService, InventoryWriteService>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();

        // ── Stored Procedure Wrapper ───────────────────────────────────────────
        services.AddScoped<IInventoryProcedures, InventoryProcedures>();

        // ── Redis ──────────────────────────────────────────────────────────────
        var redisConn = config.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
        services.AddScoped<ICacheService, RedisCacheService>();

        // ── Azure Blob Storage ─────────────────────────────────────────────────
        services.AddScoped<IBlobStorageService, BlobStorageService>();

        // ── Meilisearch ────────────────────────────────────────────────────────
        services.AddScoped<ISearchService, MeilisearchService>();

        // ── Auth / Identity Services ───────────────────────────────────────────
        services.AddScoped<ITokenService, TokenService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
