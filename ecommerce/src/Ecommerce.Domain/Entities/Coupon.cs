using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities;

public class Coupon : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? UsageLimit { get; set; }

    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public bool IsValid(decimal orderAmount)
    {
        if (ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow) return false;
        if (MinOrderAmount.HasValue && orderAmount < MinOrderAmount.Value) return false;
        return true;
    }

    public decimal CalculateDiscount(decimal orderAmount)
    {
        decimal discount = DiscountType == DiscountType.PERCENTAGE
            ? orderAmount * (DiscountValue / 100)
            : DiscountValue;

        if (MaxDiscount.HasValue && discount > MaxDiscount.Value)
            discount = MaxDiscount.Value;

        return Math.Min(discount, orderAmount);
    }
}
