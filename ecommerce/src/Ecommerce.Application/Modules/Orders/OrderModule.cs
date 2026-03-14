using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Common.Models;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Modules.Orders;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record CreateOrderRequest(long ShippingAddressId, string? CouponCode);
public record UpdateOrderStatusRequest(OrderStatus Status);

public class OrderResponse
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? CouponCode { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
    public AddressResponse? ShippingAddress { get; set; }
}

public class OrderItemResponse
{
    public long Id { get; set; }
    public string? Sku { get; set; }
    public string? ProductName { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}

public class AddressResponse
{
    public long Id { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
}

public class OrderSummaryResponse
{
    public long OrderId { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? CouponCode { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Validator ─────────────────────────────────────────────────────────────────

public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderValidator() => RuleFor(x => x.ShippingAddressId).GreaterThan(0);
}

// ── Service Interface ─────────────────────────────────────────────────────────

public interface IOrderService
{
    Task<ApiResponse<OrderResponse>> CreateOrderAsync(long userId, CreateOrderRequest request, CancellationToken ct = default);
    Task<ApiResponse<OrderResponse>> GetOrderAsync(long orderId, long userId, CancellationToken ct = default);
    Task<ApiResponse<PagedResult<OrderResponse>>> GetUserOrdersAsync(long userId, int page, int pageSize, CancellationToken ct = default);
    Task<ApiResponse<OrderResponse>> UpdateStatusAsync(long orderId, UpdateOrderStatusRequest request, CancellationToken ct = default);
    Task<ApiResponse<PagedResult<OrderSummaryResponse>>> GetAllOrderSummariesAsync(int page, int pageSize, CancellationToken ct = default);
}

// ── Service Implementation ────────────────────────────────────────────────────

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepo;
    private readonly IRepository<Cart> _cartRepo;
    private readonly IRepository<CartItem> _cartItemRepo;
    private readonly IRepository<Coupon> _couponRepo;
    private readonly IRepository<Address> _addressRepo;
    private readonly IInventoryProcedures _inventoryProcs;  // ← SP wrapper
    private readonly IOrderQueryService _orderQuery;         // ← replaces AppDbContext
    private readonly IUnitOfWork _uow;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IRepository<Order> orderRepo,
        IRepository<Cart> cartRepo,
        IRepository<CartItem> cartItemRepo,
        IRepository<Coupon> couponRepo,
        IRepository<Address> addressRepo,
        IInventoryProcedures inventoryProcs,
        IOrderQueryService orderQuery,
        IUnitOfWork uow,
        ILogger<OrderService> logger)
    {
        _orderRepo      = orderRepo;
        _cartRepo       = cartRepo;
        _cartItemRepo   = cartItemRepo;
        _couponRepo     = couponRepo;
        _addressRepo    = addressRepo;
        _inventoryProcs = inventoryProcs;
        _orderQuery     = orderQuery;
        _uow            = uow;
        _logger         = logger;
    }

    public async Task<ApiResponse<OrderResponse>> CreateOrderAsync(
        long userId, CreateOrderRequest request, CancellationToken ct = default)
    {
        var cart = await _cartRepo.Query()
            .Include(c => c.Items)
                .ThenInclude(ci => ci.Variant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart is null || !cart.Items.Any())
            return ApiResponse<OrderResponse>.Fail("Cart is empty.");

        var address = await _addressRepo.FirstOrDefaultAsync(
            a => a.Id == request.ShippingAddressId && a.UserId == userId, ct);
        if (address is null)
            return ApiResponse<OrderResponse>.Fail("Shipping address not found.");

        // ── RESERVE STOCK via sp_reserve_stock ────────────────────────────────
        var reservedItems = new List<(long VariantId, int Qty)>();
        foreach (var item in cart.Items)
        {
            var reserved = await _inventoryProcs.ReserveStockAsync(item.VariantId, item.Quantity, ct);
            if (!reserved)
            {
                foreach (var (vid, qty) in reservedItems)
                    await _inventoryProcs.ReleaseReservationAsync(vid, qty, ct);

                return ApiResponse<OrderResponse>.Fail(
                    $"Insufficient stock for SKU: {item.Variant.Sku}");
            }
            reservedItems.Add((item.VariantId, item.Quantity));
        }

        decimal subtotal = cart.Items.Sum(ci =>
            (ci.Variant.Price ?? ci.Variant.Product?.BasePrice ?? 0) * ci.Quantity);

        Coupon? coupon = null;
        decimal discount = 0;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            coupon = await _couponRepo.FirstOrDefaultAsync(c => c.Code == request.CouponCode, ct);
            if (coupon is null || !coupon.IsValid(subtotal))
            {
                foreach (var (vid, qty) in reservedItems)
                    await _inventoryProcs.ReleaseReservationAsync(vid, qty, ct);

                return ApiResponse<OrderResponse>.Fail("Invalid or expired coupon.");
            }
            discount = coupon.CalculateDiscount(subtotal);
        }

        var order = new Order
        {
            UserId            = userId,
            CouponId          = coupon?.Id,
            Status            = OrderStatus.PENDING,
            SubtotalAmount    = subtotal,
            DiscountAmount    = discount,
            TotalAmount       = subtotal - discount,
            ShippingAddressId = address.Id
        };

        await _orderRepo.AddAsync(order, ct);
        await _uow.SaveChangesAsync(ct);

        foreach (var ci in cart.Items)
        {
            var unitPrice = ci.Variant.Price ?? ci.Variant.Product?.BasePrice ?? 0;
            order.Items.Add(new OrderItem
            {
                OrderId     = order.Id,
                VariantId   = ci.VariantId,
                Sku         = ci.Variant.Sku,
                ProductName = ci.Variant.Product?.Name,
                Color       = ci.Variant.Color,
                Size        = ci.Variant.Size,
                UnitPrice   = unitPrice,
                Quantity    = ci.Quantity,
                LineTotal   = unitPrice * ci.Quantity
            });
        }

        foreach (var item in cart.Items)
            _cartItemRepo.Remove(item);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Order {OrderId} created for User {UserId} — Total:{Total}",
            order.Id, userId, order.TotalAmount);

        return await GetOrderAsync(order.Id, userId, ct);
    }

    public async Task<ApiResponse<OrderResponse>> GetOrderAsync(
        long orderId, long userId, CancellationToken ct = default)
    {
        var order = await _orderRepo.Query()
            .Include(o => o.Items)
            .Include(o => o.ShippingAddress)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct);

        if (order is null) return ApiResponse<OrderResponse>.Fail("Order not found.");

        // Enrich from v_order_summary via IOrderQueryService (no AppDbContext reference)
        var summary = await _orderQuery.GetSummaryAsync(orderId, ct);
        return ApiResponse<OrderResponse>.Ok(MapOrderResponse(order, summary));
    }

    public async Task<ApiResponse<PagedResult<OrderResponse>>> GetUserOrdersAsync(
        long userId, int page, int pageSize, CancellationToken ct = default)
    {
        // ── List from v_order_summary via IOrderQueryService ──────────────────
        var summaryPage = await _orderQuery.GetUserSummariesAsync(userId, page, pageSize, ct);

        var orderIds = summaryPage.Items.Select(s => s.OrderId).ToList();
        var orders = await _orderRepo.Query()
            .Include(o => o.Items)
            .Include(o => o.ShippingAddress)
            .Where(o => orderIds.Contains(o.Id))
            .ToListAsync(ct);

        var responses = summaryPage.Items
            .Select(s => MapOrderResponse(orders.First(o => o.Id == s.OrderId), s))
            .ToList();

        return ApiResponse<PagedResult<OrderResponse>>.Ok(new PagedResult<OrderResponse>
        {
            Items      = responses,
            TotalCount = summaryPage.TotalCount,
            Page       = page,
            PageSize   = pageSize
        });
    }

    /// <summary>
    /// Admin: all orders listed from v_order_summary via IOrderQueryService.
    /// Zero application-side joins — user, coupon, and payment data from the view.
    /// </summary>
    public async Task<ApiResponse<PagedResult<OrderSummaryResponse>>> GetAllOrderSummariesAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _orderQuery.GetAllSummariesAsync(page, pageSize, ct);

        return ApiResponse<PagedResult<OrderSummaryResponse>>.Ok(new PagedResult<OrderSummaryResponse>
        {
            Items = result.Items.Select(r => new OrderSummaryResponse
            {
                OrderId        = r.OrderId,
                UserId         = r.UserId,
                UserName       = r.UserName,
                UserEmail      = r.UserEmail,
                OrderStatus    = r.OrderStatus,
                SubtotalAmount = r.SubtotalAmount,
                DiscountAmount = r.DiscountAmount,
                TotalAmount    = r.TotalAmount,
                CouponCode     = r.CouponCode,
                PaymentStatus  = r.PaymentStatus,
                CreatedAt      = r.CreatedAt
            }),
            TotalCount = result.TotalCount,
            Page       = page,
            PageSize   = pageSize
        });
    }

    public async Task<ApiResponse<OrderResponse>> UpdateStatusAsync(
        long orderId, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        var order = await _orderRepo.Query()
            .Include(o => o.Items)
            .Include(o => o.ShippingAddress)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null) return ApiResponse<OrderResponse>.Fail("Order not found.");

        var previousStatus = order.Status;
        order.Status = request.Status;
        _orderRepo.Update(order);

        // ── RELEASE RESERVATION via sp_release_reservation on CANCELLED ───────
        if (request.Status == OrderStatus.CANCELLED && previousStatus == OrderStatus.PENDING)
        {
            foreach (var item in order.Items.Where(i => i.VariantId.HasValue && i.Quantity.HasValue))
                await _inventoryProcs.ReleaseReservationAsync(item.VariantId!.Value, item.Quantity!.Value, ct);

            _logger.LogInformation(
                "Order {OrderId} cancelled — reservations released for {Count} variant(s)",
                orderId, order.Items.Count(i => i.VariantId.HasValue));
        }

        await _uow.SaveChangesAsync(ct);

        var summary = await _orderQuery.GetSummaryAsync(orderId, ct);
        return ApiResponse<OrderResponse>.Ok(MapOrderResponse(order, summary), "Order status updated.");
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static OrderResponse MapOrderResponse(Order o, OrderSummaryRow? summary) => new()
    {
        Id             = o.Id,
        UserId         = o.UserId,
        UserName       = summary?.UserName  ?? string.Empty,
        UserEmail      = summary?.UserEmail ?? string.Empty,
        Status         = o.Status.ToString(),
        SubtotalAmount = o.SubtotalAmount,
        DiscountAmount = o.DiscountAmount,
        TotalAmount    = o.TotalAmount,
        CouponCode     = summary?.CouponCode,
        PaymentStatus  = summary?.PaymentStatus,
        CreatedAt      = o.CreatedAt,
        Items = o.Items.Select(i => new OrderItemResponse
        {
            Id          = i.Id,
            Sku         = i.Sku,
            ProductName = i.ProductName,
            Color       = i.Color,
            Size        = i.Size,
            UnitPrice   = i.UnitPrice,
            Quantity    = i.Quantity ?? 0,
            LineTotal   = i.LineTotal
        }).ToList(),
        ShippingAddress = o.ShippingAddress is null ? null : new AddressResponse
        {
            Id           = o.ShippingAddress.Id,
            AddressLine1 = o.ShippingAddress.AddressLine1,
            City         = o.ShippingAddress.City,
            State        = o.ShippingAddress.State,
            Country      = o.ShippingAddress.Country,
            PostalCode   = o.ShippingAddress.PostalCode
        }
    };
}
