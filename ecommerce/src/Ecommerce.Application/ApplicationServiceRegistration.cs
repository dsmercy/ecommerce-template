using Ecommerce.Application.Modules.Auth.Services;
using Ecommerce.Application.Modules.Auth.Validators;
using Ecommerce.Application.Modules.Carts;
using Ecommerce.Application.Modules.Categories;
using Ecommerce.Application.Modules.Coupons;
using Ecommerce.Application.Modules.Inventories;
using Ecommerce.Application.Modules.Orders;
using Ecommerce.Application.Modules.Payments;
using Ecommerce.Application.Modules.Products.Services;
using Ecommerce.Application.Modules.Reviews;
using Ecommerce.Application.Modules.Search;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ── Validators (FluentValidation) ──────────────────────────────────────
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        // ── Module Services ────────────────────────────────────────────────────
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IProductSearchService, ProductSearchService>();

        return services;
    }
}
