using System.Text.Json.Serialization;
using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Domain.Entities;
using Meilisearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Services;

public class MeilisearchService : ISearchService
{
    private readonly MeilisearchClient _client;
    private readonly ILogger<MeilisearchService> _logger;
    private const string IndexName = "products";
    private const string PrimaryKey = "id";   // must match camelCase JSON field name

    public MeilisearchService(IConfiguration config, ILogger<MeilisearchService> logger)
    {
        var host = config["Meilisearch:Host"] ?? "http://localhost:7700";
        var apiKey = config["Meilisearch:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "Meilisearch:ApiKey is not configured. " +
                "Set it in appsettings to match MEILI_MASTER_KEY on the Meilisearch container.");

        _client = new MeilisearchClient(host, apiKey);
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ENSURE INDEX SETTINGS
    // Declares searchable/filterable/sortable attributes.
    // MUST run before documents are added — otherwise filters return empty.
    // UpdateSettingsAsync is also async on the server; we wait for completion.
    // ─────────────────────────────────────────────────────────────────────────
    public async Task EnsureIndexSettingsAsync(CancellationToken ct = default)
    {
        var index = _client.Index(IndexName);

        var settings = new Settings
        {
            SearchableAttributes = new[]
            {
                "name",         // highest relevance
                "brand",
                "categoryName",
                "slug"
            },
            FilterableAttributes = new[]
            {
                "categoryId",   // required for ?categoryId= filter
                "basePrice"     // required for ?minPrice= / ?maxPrice= filter
            },
            SortableAttributes = new[]
            {
                "basePrice",
                "name"
            },
            TypoTolerance = new TypoTolerance
            {
                Enabled = true,
                MinWordSizeForTypos = new TypoTolerance.TypoSize
                {
                    OneTypo = 5,
                    TwoTypos = 9
                }
            }
        };

        var taskInfo = await index.UpdateSettingsAsync(settings);
        _logger.LogInformation(
            "Meilisearch settings task enqueued. TaskUid={TaskUid}", taskInfo.TaskUid);

        // Wait for settings to be applied before indexing documents
        var result = await _client.WaitForTaskAsync(taskInfo.TaskUid, cancellationToken: ct);
        _logger.LogInformation(
            "Meilisearch settings task completed. Status={Status}", result.Status);

        if (result.Status == TaskInfoStatus.Failed)
            _logger.LogError("Meilisearch settings task failed: {Error}", result.Error);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BULK INDEX  (used by SyncAllProductsAsync — faster than one-by-one)
    // ─────────────────────────────────────────────────────────────────────────
    public async Task BulkIndexProductsAsync(
        IEnumerable<Product> products, CancellationToken ct = default)
    {
        try
        {
            var index = _client.Index(IndexName);
            var documents = products.Select(MapToDto).ToList();

            if (!documents.Any())
            {
                _logger.LogWarning("BulkIndexProducts called with 0 documents — nothing to index.");
                return;
            }

            // Pass primaryKey explicitly so Meilisearch knows which field is the document ID.
            // The SDK serialises ProductSearchDto with camelCase, so "Id" → "id".
            var taskInfo = await index.AddDocumentsAsync(
                documents, primaryKey: PrimaryKey, cancellationToken: ct);

            _logger.LogInformation(
                "Meilisearch bulk index task enqueued. TaskUid={TaskUid} Documents={Count}",
                taskInfo.TaskUid, documents.Count);

            // Wait for indexing to complete so the caller knows documents are searchable
            var result = await _client.WaitForTaskAsync(
                taskInfo.TaskUid, timeoutMs: 30000, cancellationToken: ct);

            _logger.LogInformation(
                "Meilisearch bulk index task completed. Status={Status}", result.Status);

            if (result.Status == TaskInfoStatus.Failed)
                _logger.LogError(
                    "Meilisearch bulk index task failed: {Error}", result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk index failed");
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INDEX ONE PRODUCT  (called on create/update)
    // Does NOT wait — single-document operations complete near-instantly
    // ─────────────────────────────────────────────────────────────────────────
    public async Task IndexProductAsync(Product product, CancellationToken ct = default)
    {
        try
        {
            var index = _client.Index(IndexName);
            var document = MapToDto(product);

            var taskInfo = await index.AddDocumentsAsync(
                new[] { document }, primaryKey: PrimaryKey, cancellationToken: ct);

            _logger.LogInformation(
                "Product index task enqueued. ProductId={ProductId} TaskUid={TaskUid}",
                product.Id, taskInfo.TaskUid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index product {ProductId}", product.Id);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DELETE ONE PRODUCT
    // ─────────────────────────────────────────────────────────────────────────
    public async Task DeleteProductIndexAsync(long productId, CancellationToken ct = default)
    {
        try
        {
            var index = _client.Index(IndexName);
            var taskInfo = await index.DeleteOneDocumentAsync(productId.ToString(), ct);

            _logger.LogInformation(
                "Product delete task enqueued. ProductId={ProductId} TaskUid={TaskUid}",
                productId, taskInfo.TaskUid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete product {ProductId} from index", productId);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SEARCH
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<SearchResult> SearchProductsAsync(
        string query, ProductSearchFilter filter, CancellationToken ct = default)
    {
        try
        {
            var index = _client.Index(IndexName);

            var searchQuery = new SearchQuery
            {
                Limit = filter.PageSize,
                Offset = (filter.Page - 1) * filter.PageSize,
                Filter = BuildFilter(filter)
            };

            var raw = await index.SearchAsync<ProductSearchDto>(query, searchQuery, ct);
            var result = (Meilisearch.SearchResult<ProductSearchDto>)raw;

            return new SearchResult
            {
                Items = result.Hits,
                TotalHits = result.EstimatedTotalHits
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Meilisearch query failed: {Query}", query);
            return new SearchResult();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FILTER BUILDER
    // Fields must be listed in filterableAttributes in EnsureIndexSettingsAsync.
    // ─────────────────────────────────────────────────────────────────────────
    private static string? BuildFilter(ProductSearchFilter filter)
    {
        var parts = new List<string>();

        if (filter.CategoryId.HasValue)
            parts.Add($"categoryId = {filter.CategoryId.Value}");
        if (filter.MinPrice.HasValue)
            parts.Add($"basePrice >= {filter.MinPrice.Value}");
        if (filter.MaxPrice.HasValue)
            parts.Add($"basePrice <= {filter.MaxPrice.Value}");

        return parts.Count > 0 ? string.Join(" AND ", parts) : null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MAPPER
    // ─────────────────────────────────────────────────────────────────────────
    private static ProductSearchDto MapToDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Brand = product.Brand,
        BasePrice = product.BasePrice,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name,
        Slug = product.Slug,
        PrimaryImageUrl = product.Images
            .Where(i => i.IsPrimary)
            .Select(i => i.ImageUrl)
            .FirstOrDefault()
            ?? product.Images.Select(i => i.ImageUrl).FirstOrDefault()
    };
}