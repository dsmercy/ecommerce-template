using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Common.Models;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Modules.Carts;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record AddToCartRequest(long VariantId, int Quantity);
public record UpdateCartItemRequest(int Quantity);

public class CartResponse
{
    public long CartId { get; set; }
    public long UserId { get; set; }
    public List<CartItemResponse> Items { get; set; } = new();
    public decimal TotalPrice { get; set; }
}

public class CartItemResponse
{
    public long Id { get; set; }
    public long VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public decimal? UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}

// ── Validators ─────────────────────────────────────────────────────────────────

public class AddToCartValidator : AbstractValidator<AddToCartRequest>
{
    public AddToCartValidator()
    {
        RuleFor(x => x.VariantId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public class UpdateCartItemValidator : AbstractValidator<UpdateCartItemRequest>
{
    public UpdateCartItemValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

// ── Service ────────────────────────────────────────────────────────────────────

public interface ICartService
{
    Task<ApiResponse<CartResponse>> GetCartAsync(long userId, CancellationToken ct = default);
    Task<ApiResponse<CartResponse>> AddItemAsync(long userId, AddToCartRequest request, CancellationToken ct = default);
    Task<ApiResponse<CartResponse>> UpdateItemAsync(long userId, long cartItemId, UpdateCartItemRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> RemoveItemAsync(long userId, long cartItemId, CancellationToken ct = default);
    Task<ApiResponse<bool>> ClearCartAsync(long userId, CancellationToken ct = default);
}

public class CartService : ICartService
{
    private readonly IRepository<Cart> _cartRepo;
    private readonly IRepository<CartItem> _cartItemRepo;
    private readonly IRepository<ProductVariant> _variantRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cache;
    private readonly ILogger<CartService> _logger;

    private string CartCacheKey(long userId) => $"cart:{userId}";

    public CartService(
        IRepository<Cart> cartRepo,
        IRepository<CartItem> cartItemRepo,
        IRepository<ProductVariant> variantRepo,
        IUnitOfWork uow,
        ICacheService cache,
        ILogger<CartService> logger)
    {
        _cartRepo = cartRepo;
        _cartItemRepo = cartItemRepo;
        _variantRepo = variantRepo;
        _uow = uow;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ApiResponse<CartResponse>> GetCartAsync(long userId, CancellationToken ct = default)
    {
        var cached = await _cache.GetAsync<CartResponse>(CartCacheKey(userId), ct);
        if (cached is not null) return ApiResponse<CartResponse>.Ok(cached);

        var cart = await GetOrCreateCartAsync(userId, ct);
        var response = await BuildCartResponseAsync(cart, ct);
        await _cache.SetAsync(CartCacheKey(userId), response, TimeSpan.FromMinutes(10), ct);
        return ApiResponse<CartResponse>.Ok(response);
    }

    public async Task<ApiResponse<CartResponse>> AddItemAsync(long userId, AddToCartRequest request, CancellationToken ct = default)
    {
        var variant = await _variantRepo.Query()
            .Include(v => v.Inventory)
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == request.VariantId, ct);

        if (variant is null)
            return ApiResponse<CartResponse>.Fail("Variant not found.");

        if (variant.Inventory is null || variant.Inventory.AvailableQuantity < request.Quantity)
            return ApiResponse<CartResponse>.Fail("Insufficient stock.");

        var cart = await GetOrCreateCartAsync(userId, ct);

        var existingItem = await _cartItemRepo.FirstOrDefaultAsync(
            ci => ci.CartId == cart.Id && ci.VariantId == request.VariantId, ct);

        if (existingItem is not null)
        {
            existingItem.Quantity += request.Quantity;
            _cartItemRepo.Update(existingItem);
        }
        else
        {
            await _cartItemRepo.AddAsync(new CartItem
            {
                CartId = cart.Id,
                VariantId = request.VariantId,
                Quantity = request.Quantity
            }, ct);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        _cartRepo.Update(cart);
        await _uow.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CartCacheKey(userId), ct);

        return await GetCartAsync(userId, ct);
    }

    public async Task<ApiResponse<CartResponse>> UpdateItemAsync(long userId, long cartItemId, UpdateCartItemRequest request, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, ct);
        var item = await _cartItemRepo.FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.CartId == cart.Id, ct);
        if (item is null)
            return ApiResponse<CartResponse>.Fail("Cart item not found.");

        item.Quantity = request.Quantity;
        _cartItemRepo.Update(item);
        await _uow.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CartCacheKey(userId), ct);

        return await GetCartAsync(userId, ct);
    }

    public async Task<ApiResponse<bool>> RemoveItemAsync(long userId, long cartItemId, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, ct);
        var item = await _cartItemRepo.FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.CartId == cart.Id, ct);
        if (item is null)
            return ApiResponse<bool>.Fail("Cart item not found.");

        _cartItemRepo.Remove(item);
        await _uow.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CartCacheKey(userId), ct);
        return ApiResponse<bool>.Ok(true, "Item removed.");
    }

    public async Task<ApiResponse<bool>> ClearCartAsync(long userId, CancellationToken ct = default)
    {
        var cart = await _cartRepo.Query()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart is not null)
        {
            foreach (var item in cart.Items) _cartItemRepo.Remove(item);
            await _uow.SaveChangesAsync(ct);
            await _cache.RemoveAsync(CartCacheKey(userId), ct);
        }
        return ApiResponse<bool>.Ok(true, "Cart cleared.");
    }

    private async Task<Cart> GetOrCreateCartAsync(long userId, CancellationToken ct)
    {
        var cart = await _cartRepo.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (cart is null)
        {
            cart = new Cart { UserId = userId };
            await _cartRepo.AddAsync(cart, ct);
            await _uow.SaveChangesAsync(ct);
        }
        return cart;
    }

    private async Task<CartResponse> BuildCartResponseAsync(Cart cart, CancellationToken ct)
    {
        var items = await _cartItemRepo.Query()
            .Include(ci => ci.Variant).ThenInclude(v => v.Product)
            .Where(ci => ci.CartId == cart.Id)
            .ToListAsync(ct);

        var itemResponses = items.Select(ci => new CartItemResponse
        {
            Id = ci.Id,
            VariantId = ci.VariantId,
            Sku = ci.Variant.Sku,
            ProductName = ci.Variant.Product?.Name,
            Color = ci.Variant.Color,
            Size = ci.Variant.Size,
            UnitPrice = ci.Variant.Price,
            Quantity = ci.Quantity,
            LineTotal = (ci.Variant.Price ?? 0) * ci.Quantity
        }).ToList();

        return new CartResponse
        {
            CartId = cart.Id,
            UserId = cart.UserId,
            Items = itemResponses,
            TotalPrice = itemResponses.Sum(i => i.LineTotal)
        };
    }
}
