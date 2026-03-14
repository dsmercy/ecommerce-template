using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Common.Models;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Modules.Search;

public interface IProductSearchService
{
    Task<ApiResponse<SearchResult>> SearchAsync(ProductSearchFilter filter, CancellationToken ct = default);
    Task<ApiResponse<SyncResult>> SyncAllProductsAsync(CancellationToken ct = default);
}

public class SyncResult
{
    public int ProductsFound { get; set; }
    public string IndexStatus { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ProductSearchService : IProductSearchService
{
    private readonly ISearchService _searchService;
    private readonly IRepository<Product> _productRepo;
    private readonly ILogger<ProductSearchService> _logger;

    public ProductSearchService(
        ISearchService searchService,
        IRepository<Product> productRepo,
        ILogger<ProductSearchService> logger)
    {
        _searchService = searchService;
        _productRepo = productRepo;
        _logger = logger;
    }

    public async Task<ApiResponse<SearchResult>> SearchAsync(
        ProductSearchFilter filter, CancellationToken ct = default)
    {
        var result = await _searchService.SearchProductsAsync(filter.Query ?? "", filter, ct);
        return ApiResponse<SearchResult>.Ok(result);
    }

    /// <summary>
    /// Full re-sync. Steps:
    ///   1. Apply index settings (searchable + filterable attributes) — waits for completion.
    ///   2. Load all active products from DB with Category + Images.
    ///   3. Bulk-index them in one request — waits for completion.
    /// Returns a SyncResult with document count and task status for diagnostics.
    /// </summary>
    public async Task<ApiResponse<SyncResult>> SyncAllProductsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Meilisearch full sync...");

        // Step 1 — apply settings and wait
        await _searchService.EnsureIndexSettingsAsync(ct);
        _logger.LogInformation("Index settings applied.");

        // Step 2 — load products
        var products = await _productRepo.Query()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.IsActive)
            .ToListAsync(ct);

        _logger.LogInformation("Found {Count} active products to index.", products.Count);

        if (products.Count == 0)
        {
            return ApiResponse<SyncResult>.Ok(new SyncResult
            {
                ProductsFound = 0,
                IndexStatus = "skipped",
                Message = "No active products found in database. Run the seed script first."
            });
        }

        // Step 3 — bulk index and wait for task completion
        await _searchService.BulkIndexProductsAsync(products, ct);

        var syncResult = new SyncResult
        {
            ProductsFound = products.Count,
            IndexStatus = "succeeded",
            Message = $"Successfully indexed {products.Count} products. Search is ready."
        };

        _logger.LogInformation("Meilisearch sync complete. {Count} products indexed.", products.Count);
        return ApiResponse<SyncResult>.Ok(syncResult);
    }
}