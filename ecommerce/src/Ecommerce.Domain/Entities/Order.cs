using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities;

public class Order : BaseEntity
{
    public long UserId { get; set; }
    public long? CouponId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PENDING;
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public long? ShippingAddressId { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Address? ShippingAddress { get; set; }
    public Coupon? Coupon { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class OrderItem : BaseEntity
{
    public long OrderId { get; set; }
    public long? VariantId { get; set; }
    public string? Sku { get; set; }
    public string? ProductName { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public decimal UnitPrice { get; set; }
    public int? Quantity { get; set; }
    public decimal LineTotal { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}
