namespace Ecommerce.Domain.Entities;

public class Cart : BaseEntity
{
    public long UserId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem : BaseEntity
{
    public long CartId { get; set; }
    public long VariantId { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Cart Cart { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}
