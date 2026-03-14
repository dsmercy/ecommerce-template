namespace Ecommerce.Domain.Entities;

public class Product : AuditableEntity
{
    public long? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public decimal? BasePrice { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Category? Category { get; set; }
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
