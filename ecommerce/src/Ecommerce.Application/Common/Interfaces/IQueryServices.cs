using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Modules.Inventories;
using Ecommerce.Application.Modules.Orders;
using Ecommerce.Application.Modules.Products.DTOs;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Common.Interfaces;

// =============================================================================
// PRODUCT QUERY INTERFACE
// Abstracts v_product_catalogue view queries.
// Defined here (Application), implemented in Infrastructure.
// =============================================================================
public interface IProductQueryService
{
    /// <summary>
    /// Reads from v_product_catalogue view.
    /// Returns paginated, filtered active products with category + primary image.
    /// </summary>
    Task<PagedResult<ProductListResponse>> GetCatalogueAsync(ProductFilterParams filter, CancellationToken ct = default);

    /// <summary>
    /// Reads a single row from v_product_catalogue by product ID.
    /// Used to enrich GetById responses with primary image URL from the view.
    /// </summary>
    Task<ProductCatalogueRow?> GetCatalogueRowAsync(long productId, CancellationToken ct = default);
}

/// <summary>Projection of a single v_product_catalogue row.</summary>
public class ProductCatalogueRow
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Brand { get; set; }
    public decimal? BasePrice { get; set; }
    public bool IsActive { get; set; }
    public long? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategorySlug { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

// =============================================================================
// INVENTORY QUERY INTERFACE
// Abstracts v_inventory_available view queries.
// =============================================================================
public interface IInventoryQueryService
{
    /// <summary>
    /// Reads one row from v_inventory_available for a given variant.
    /// Returns null when no inventory record exists for the variant.
    /// </summary>
    Task<InventoryAvailableRow?> GetByVariantAsync(long variantId, CancellationToken ct = default);

    /// <summary>
    /// Reads all rows from v_inventory_available for all variants of a product.
    /// </summary>
    Task<IEnumerable<InventoryAvailableRow>> GetByProductAsync(long productId, CancellationToken ct = default);
}

/// <summary>Projection of a single v_inventory_available row.</summary>
public class InventoryAvailableRow
{
    public long Id { get; set; }
    public long VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public int StockQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// =============================================================================
// ORDER QUERY INTERFACE
// Abstracts v_order_summary view queries.
// =============================================================================
public interface IOrderQueryService
{
    /// <summary>
    /// Reads one row from v_order_summary for a given order ID.
    /// Returns null when no matching order exists.
    /// </summary>
    Task<OrderSummaryRow?> GetSummaryAsync(long orderId, CancellationToken ct = default);

    /// <summary>
    /// Reads paginated v_order_summary rows filtered by userId.
    /// Used for customer order history.
    /// </summary>
    Task<PagedResult<OrderSummaryRow>> GetUserSummariesAsync(long userId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Reads paginated v_order_summary rows for all users.
    /// Used for admin order management.
    /// </summary>
    Task<PagedResult<OrderSummaryRow>> GetAllSummariesAsync(int page, int pageSize, CancellationToken ct = default);
}

/// <summary>Projection of a single v_order_summary row.</summary>
public class OrderSummaryRow
{
    public long OrderId { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CouponCode { get; set; }
    public string? PaymentStatus { get; set; }
}

// =============================================================================
// INVENTORY WRITE INTERFACE
// Wraps the one write operation InventoryService needs (admin restock).
// Defined in Application, implemented in Infrastructure via IRepository<Inventory>.
// Keeps InventoryService completely free of IRepository / AppDbContext.
// =============================================================================
public interface IInventoryWriteService
{
    /// <summary>
    /// Directly sets stock_quantity for a variant.
    /// Not a stored procedure — this is an admin manual override.
    /// </summary>
    Task SetStockQuantityAsync(long variantId, int stockQuantity, CancellationToken ct = default);
}
