namespace Ecommerce.Application.Modules.Products.DTOs;

public record CreateProductRequest(
    long? CategoryId,
    string Name,
    string? Description,
    string? Brand,
    decimal? BasePrice,
    bool IsActive = true
);

public record UpdateProductRequest(
    long? CategoryId,
    string? Name,
    string? Description,
    string? Brand,
    decimal? BasePrice,
    bool? IsActive
);

public record CreateVariantRequest(
    string Sku,
    string? Color,
    string? Size,
    decimal? Price,
    int InitialStock = 0
);

public class ProductResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public decimal? BasePrice { get; set; }
    public bool IsActive { get; set; }
    public string? CategoryName { get; set; }
    public long? CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ProductImageResponse> Images { get; set; } = new();
    public List<VariantResponse> Variants { get; set; } = new();
}

public class ProductImageResponse
{
    public long Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class VariantResponse
{
    public long Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Size { get; set; }
    public decimal? Price { get; set; }
    public int StockQuantity { get; set; }
    public int AvailableQuantity { get; set; }
}

public class ProductListResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Brand { get; set; }
    public decimal? BasePrice { get; set; }
    public bool IsActive { get; set; }
    public long? CategoryId { get; set; }    // ? added
    public string? CategoryName { get; set; }
    public string? PrimaryImageUrl { get; set; }
}

public class ProductFilterParams
{
    public long? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Brand { get; set; }
    public bool? IsActive { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
