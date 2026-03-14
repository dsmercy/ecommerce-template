namespace Ecommerce.Domain.Entities;

public class ProductImage : BaseEntity
{
    public long ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; } = false;

    // Navigation
    public Product Product { get; set; } = null!;
}

public class ProductVariant : BaseEntity
{
    public long ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Size { get; set; }
    public decimal? Price { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
    public Inventory? Inventory { get; set; }
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public class Inventory : BaseEntity
{
    public long VariantId { get; set; }
    public int StockQuantity { get; set; } = 0;
    public int ReservedQuantity { get; set; } = 0;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int AvailableQuantity => StockQuantity - ReservedQuantity;

    // Navigation
    public ProductVariant Variant { get; set; } = null!;
}
