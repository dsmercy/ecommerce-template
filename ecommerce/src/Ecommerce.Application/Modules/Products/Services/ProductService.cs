using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Modules.Products.DTOs;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Modules.Products.Services;

public interface IProductService
{
    Task<ApiResponse<PagedResult<ProductListResponse>>> GetAllAsync(ProductFilterParams filter, CancellationToken ct = default);
    Task<ApiResponse<ProductResponse>> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ApiResponse<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ApiResponse<ProductResponse>> UpdateAsync(long id, UpdateProductRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteAsync(long id, CancellationToken ct = default);
    Task<ApiResponse<VariantResponse>> AddVariantAsync(long productId, CreateVariantRequest request, CancellationToken ct = default);
    Task<ApiResponse<string>> UploadImageAsync(long productId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
}

public class ProductService : IProductService
{
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<ProductVariant> _variantRepo;
    private readonly IRepository<Inventory> _inventoryRepo;
    private readonly IRepository<ProductImage> _imageRepo;
    private readonly IProductQueryService _productQuery;      // ← replaces AppDbContext
    private readonly IInventoryQueryService _inventoryQuery;  // ← replaces AppDbContext
    private readonly IUnitOfWork _uow;
    private readonly IBlobStorageService _blobStorage;
    private readonly ISearchService _searchService;
    private readonly ICacheService _cache;
    private readonly ILogger<ProductService> _logger;

    private const string CachePrefix = "product:";

    public ProductService(
        IRepository<Product> productRepo,
        IRepository<ProductVariant> variantRepo,
        IRepository<Inventory> inventoryRepo,
        IRepository<ProductImage> imageRepo,
        IProductQueryService productQuery,
        IInventoryQueryService inventoryQuery,
        IUnitOfWork uow,
        IBlobStorageService blobStorage,
        ISearchService searchService,
        ICacheService cache,
        ILogger<ProductService> logger)
    {
        _productRepo    = productRepo;
        _variantRepo    = variantRepo;
        _inventoryRepo  = inventoryRepo;
        _imageRepo      = imageRepo;
        _productQuery   = productQuery;
        _inventoryQuery = inventoryQuery;
        _uow            = uow;
        _blobStorage    = blobStorage;
        _searchService  = searchService;
        _cache          = cache;
        _logger         = logger;
    }

    /// <summary>
    /// Listing is served from v_product_catalogue via IProductQueryService.
    /// No manual category joins or image subqueries in application code.
    /// </summary>
    public async Task<ApiResponse<PagedResult<ProductListResponse>>> GetAllAsync(
        ProductFilterParams filter, CancellationToken ct = default)
    {
        var result = await _productQuery.GetCatalogueAsync(filter, ct);
        return ApiResponse<PagedResult<ProductListResponse>>.Ok(result);
    }

    /// <summary>
    /// Full product detail — normalised tables for variants/images/inventory,
    /// enriched with primary image URL from v_product_catalogue via IProductQueryService.
    /// Result is cached in Redis for 15 minutes.
    /// </summary>
    public async Task<ApiResponse<ProductResponse>> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var cacheKey = $"{CachePrefix}{id}";
        var cached   = await _cache.GetAsync<ProductResponse>(cacheKey, ct);
        if (cached is not null) return ApiResponse<ProductResponse>.Ok(cached);

        var product = await _productRepo.Query()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants).ThenInclude(v => v.Inventory)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
            return ApiResponse<ProductResponse>.Fail("Product not found.");

        // Enrich with primary image URL resolved by the view
        var viewRow = await _productQuery.GetCatalogueRowAsync(id, ct);

        var response = MapToProductResponse(product, viewRow?.PrimaryImageUrl);
        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15), ct);
        return ApiResponse<ProductResponse>.Ok(response);
    }

    public async Task<ApiResponse<ProductResponse>> CreateAsync(
        CreateProductRequest request, CancellationToken ct = default)
    {
        var slug = GenerateSlug(request.Name);
        if (await _productRepo.AnyAsync(p => p.Slug == slug, ct))
            slug = $"{slug}-{Guid.NewGuid().ToString()[..8]}";

        var product = new Product
        {
            CategoryId  = request.CategoryId,
            Name        = request.Name,
            Slug        = slug,
            Description = request.Description,
            Brand       = request.Brand,
            BasePrice   = request.BasePrice,
            IsActive    = request.IsActive
        };

        await _productRepo.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        var full = await _productRepo.Query()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants).ThenInclude(v => v.Inventory)
            .FirstOrDefaultAsync(p => p.Id == product.Id, ct);

        await _searchService.IndexProductAsync(full!, ct);
        _logger.LogInformation("Product created: {ProductId} — {Name}", product.Id, product.Name);

        return ApiResponse<ProductResponse>.Ok(MapToProductResponse(full!, null), "Product created.");
    }

    public async Task<ApiResponse<ProductResponse>> UpdateAsync(
        long id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _productRepo.GetByIdAsync(id, ct);
        if (product is null) return ApiResponse<ProductResponse>.Fail("Product not found.");

        if (request.Name        is not null) product.Name        = request.Name;
        if (request.Description is not null) product.Description = request.Description;
        if (request.Brand       is not null) product.Brand       = request.Brand;
        if (request.BasePrice.HasValue)      product.BasePrice   = request.BasePrice;
        if (request.IsActive.HasValue)       product.IsActive    = request.IsActive.Value;
        if (request.CategoryId.HasValue)     product.CategoryId  = request.CategoryId;

        _productRepo.Update(product);
        await _uow.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"{CachePrefix}{id}", ct);

        var full = await _productRepo.Query()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants).ThenInclude(v => v.Inventory)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        await _searchService.IndexProductAsync(full!, ct);
        return ApiResponse<ProductResponse>.Ok(MapToProductResponse(full!, null), "Product updated.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(long id, CancellationToken ct = default)
    {
        var product = await _productRepo.GetByIdAsync(id, ct);
        if (product is null) return ApiResponse<bool>.Fail("Product not found.");

        product.DeletedAt = DateTime.UtcNow;
        _productRepo.Update(product);
        await _uow.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"{CachePrefix}{id}", ct);
        await _searchService.DeleteProductIndexAsync(id, ct);

        _logger.LogInformation("Product soft-deleted: {ProductId}", id);
        return ApiResponse<bool>.Ok(true, "Product deleted.");
    }

    public async Task<ApiResponse<VariantResponse>> AddVariantAsync(
        long productId, CreateVariantRequest request, CancellationToken ct = default)
    {
        if (!await _productRepo.AnyAsync(p => p.Id == productId, ct))
            return ApiResponse<VariantResponse>.Fail("Product not found.");

        if (await _variantRepo.AnyAsync(v => v.Sku == request.Sku, ct))
            return ApiResponse<VariantResponse>.Fail("SKU already exists.");

        var variant = new ProductVariant
        {
            ProductId = productId,
            Sku       = request.Sku,
            Color     = request.Color,
            Size      = request.Size,
            Price     = request.Price
        };

        await _variantRepo.AddAsync(variant, ct);
        await _uow.SaveChangesAsync(ct);

        var inventory = new Inventory { VariantId = variant.Id, StockQuantity = request.InitialStock };
        await _inventoryRepo.AddAsync(inventory, ct);
        await _uow.SaveChangesAsync(ct);

        await _cache.RemoveAsync($"{CachePrefix}{productId}", ct);

        // Read from v_inventory_available via IInventoryQueryService (no AppDbContext)
        var invRow = await _inventoryQuery.GetByVariantAsync(variant.Id, ct);

        return ApiResponse<VariantResponse>.Ok(new VariantResponse
        {
            Id                = variant.Id,
            Sku               = variant.Sku,
            Color             = variant.Color,
            Size              = variant.Size,
            Price             = variant.Price,
            StockQuantity     = invRow?.StockQuantity    ?? request.InitialStock,
            AvailableQuantity = invRow?.AvailableQuantity ?? request.InitialStock
        }, "Variant added.");
    }

    public async Task<ApiResponse<string>> UploadImageAsync(
        long productId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        if (!await _productRepo.AnyAsync(p => p.Id == productId, ct))
            return ApiResponse<string>.Fail("Product not found.");

        var blobName = $"products/{productId}/{Guid.NewGuid()}-{fileName}";
        var url      = await _blobStorage.UploadAsync(fileStream, blobName, contentType, ct);

        var hasPrimary = await _imageRepo.AnyAsync(i => i.ProductId == productId && i.IsPrimary, ct);
        var image      = new ProductImage { ProductId = productId, ImageUrl = url, IsPrimary = !hasPrimary };
        await _imageRepo.AddAsync(image, ct);
        await _uow.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"{CachePrefix}{productId}", ct);

        return ApiResponse<string>.Ok(url, "Image uploaded.");
    }

    private static ProductResponse MapToProductResponse(Product p, string? primaryImageUrl) => new()
    {
        Id           = p.Id,
        Name         = p.Name,
        Slug         = p.Slug,
        Description  = p.Description,
        Brand        = p.Brand,
        BasePrice    = p.BasePrice,
        IsActive     = p.IsActive,
        CategoryId   = p.CategoryId,
        CategoryName = p.Category?.Name,
        CreatedAt    = p.CreatedAt,
        Images = p.Images.Select(i => new ProductImageResponse
        {
            Id = i.Id, ImageUrl = i.ImageUrl, IsPrimary = i.IsPrimary
        }).ToList(),
        Variants = p.Variants.Select(v => new VariantResponse
        {
            Id                = v.Id,
            Sku               = v.Sku,
            Color             = v.Color,
            Size              = v.Size,
            Price             = v.Price,
            StockQuantity     = v.Inventory?.StockQuantity    ?? 0,
            AvailableQuantity = v.Inventory?.AvailableQuantity ?? 0
        }).ToList()
    };

    private static string GenerateSlug(string name)
        => name.ToLowerInvariant().Replace(" ", "-").Replace("'", "").Replace(",", "");
}
