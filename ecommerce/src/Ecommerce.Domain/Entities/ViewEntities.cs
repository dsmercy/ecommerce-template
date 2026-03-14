namespace Ecommerce.Domain.Entities;

/// <summary>
/// Maps to SQL view: v_inventory_available
/// Available = stock_quantity - reserved_quantity
/// </summary>
public class InventoryAvailableView
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

/// <summary>
/// Maps to SQL view: v_product_catalogue
/// Active products joined with category and primary image URL
/// </summary>
public class ProductCatalogueView
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

/// <summary>
/// Maps to SQL view: v_order_summary
/// Orders joined with user, coupon, and latest payment status
/// </summary>
public class OrderSummaryView
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
