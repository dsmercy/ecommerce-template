namespace Ecommerce.Domain.Entities;

public class Review : BaseEntity
{
    public long UserId { get; set; }
    public long ProductId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
