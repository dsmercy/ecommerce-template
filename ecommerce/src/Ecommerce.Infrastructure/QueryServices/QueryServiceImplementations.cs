using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Modules.Products.DTOs;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.QueryServices;

// =============================================================================
// PRODUCT QUERY SERVICE
// Implements IProductQueryService using v_product_catalogue view via AppDbContext.
// AppDbContext lives in Infrastructure — this is the correct layer for it.
// =============================================================================
public class ProductQueryService : IProductQueryService
{
    private readonly AppDbContext _db;

    public ProductQueryService(AppDbContext db) => _db = db;

    public async Task<PagedResult<ProductListResponse>> GetCatalogueAsync(
        ProductFilterParams filter, CancellationToken ct = default)
    {
        var query = _db.ProductCatalogue.AsQueryable();

        if (filter.CategoryId.HasValue)
            query = query.Where(v => v.CategoryId == filter.CategoryId);
        if (filter.MinPrice.HasValue)
            query = query.Where(v => v.BasePrice >= filter.MinPrice);
        if (filter.MaxPrice.HasValue)
            query = query.Where(v => v.BasePrice <= filter.MaxPrice);
        if (!string.IsNullOrWhiteSpace(filter.Brand))
            query = query.Where(v => v.Brand == filter.Brand);
        if (filter.IsActive.HasValue)
            query = query.Where(v => v.IsActive == filter.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(v =>
                v.Name.Contains(filter.Search) ||
                (v.Brand != null && v.Brand.Contains(filter.Search)));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(v => new ProductListResponse
            {
                Id              = v.Id,
                Name            = v.Name,
                Slug            = v.Slug,
                Brand           = v.Brand,
                BasePrice       = v.BasePrice,
                IsActive        = v.IsActive,
                CategoryId      = v.CategoryId,
                CategoryName    = v.CategoryName,
                PrimaryImageUrl = v.PrimaryImageUrl
            })
            .ToListAsync(ct);

        return new PagedResult<ProductListResponse>
        {
            Items     = items,
            TotalCount = total,
            Page      = filter.Page,
            PageSize  = filter.PageSize
        };
    }

    public async Task<ProductCatalogueRow?> GetCatalogueRowAsync(long productId, CancellationToken ct = default)
    {
        var row = await _db.ProductCatalogue
            .FirstOrDefaultAsync(v => v.Id == productId, ct);

        if (row is null) return null;

        return new ProductCatalogueRow
        {
            Id             = row.Id,
            Name           = row.Name,
            Slug           = row.Slug,
            Brand          = row.Brand,
            BasePrice      = row.BasePrice,
            IsActive       = row.IsActive,
            CategoryId     = row.CategoryId,
            CategoryName   = row.CategoryName,
            CategorySlug   = row.CategorySlug,
            PrimaryImageUrl = row.PrimaryImageUrl,
            CreatedAt      = row.CreatedAt
        };
    }
}

// =============================================================================
// INVENTORY QUERY SERVICE
// Implements IInventoryQueryService using v_inventory_available view.
// =============================================================================
public class InventoryQueryService : IInventoryQueryService
{
    private readonly AppDbContext _db;

    public InventoryQueryService(AppDbContext db) => _db = db;

    public async Task<InventoryAvailableRow?> GetByVariantAsync(long variantId, CancellationToken ct = default)
    {
        var row = await _db.InventoryAvailable
            .FirstOrDefaultAsync(v => v.VariantId == variantId, ct);

        return row is null ? null : MapRow(row);
    }

    public async Task<IEnumerable<InventoryAvailableRow>> GetByProductAsync(long productId, CancellationToken ct = default)
    {
        var rows = await _db.InventoryAvailable
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.Sku)
            .ToListAsync(ct);

        return rows.Select(MapRow);
    }

    private static InventoryAvailableRow MapRow(Domain.Entities.InventoryAvailableView v) => new()
    {
        Id                = v.Id,
        VariantId         = v.VariantId,
        Sku               = v.Sku,
        ProductId         = v.ProductId,
        StockQuantity     = v.StockQuantity,
        ReservedQuantity  = v.ReservedQuantity,
        AvailableQuantity = v.AvailableQuantity,
        UpdatedAt         = v.UpdatedAt
    };
}

// =============================================================================
// ORDER QUERY SERVICE
// Implements IOrderQueryService using v_order_summary view.
// =============================================================================
public class OrderQueryService : IOrderQueryService
{
    private readonly AppDbContext _db;

    public OrderQueryService(AppDbContext db) => _db = db;

    public async Task<OrderSummaryRow?> GetSummaryAsync(long orderId, CancellationToken ct = default)
    {
        var row = await _db.OrderSummary
            .FirstOrDefaultAsync(v => v.OrderId == orderId, ct);

        return row is null ? null : MapRow(row);
    }

    public async Task<PagedResult<OrderSummaryRow>> GetUserSummariesAsync(
        long userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.OrderSummary
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAt);

        return await ProjectPageAsync(query, page, pageSize, ct);
    }

    public async Task<PagedResult<OrderSummaryRow>> GetAllSummariesAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.OrderSummary.OrderByDescending(v => v.CreatedAt);
        return await ProjectPageAsync(query, page, pageSize, ct);
    }

    private static async Task<PagedResult<OrderSummaryRow>> ProjectPageAsync(
        IQueryable<Domain.Entities.OrderSummaryView> query,
        int page, int pageSize, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var rows  = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<OrderSummaryRow>
        {
            Items      = rows.Select(MapRow),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    private static OrderSummaryRow MapRow(Domain.Entities.OrderSummaryView v) => new()
    {
        OrderId        = v.OrderId,
        UserId         = v.UserId,
        UserName       = v.UserName,
        UserEmail      = v.UserEmail,
        OrderStatus    = v.OrderStatus,
        SubtotalAmount = v.SubtotalAmount,
        DiscountAmount = v.DiscountAmount,
        TotalAmount    = v.TotalAmount,
        CreatedAt      = v.CreatedAt,
        CouponCode     = v.CouponCode,
        PaymentStatus  = v.PaymentStatus
    };
}

// =============================================================================
// INVENTORY WRITE SERVICE
// Implements IInventoryWriteService — the only write operation InventoryService
// needs. Uses IRepository<Inventory> + IUnitOfWork (both in Infrastructure).
// Application layer sees only the IInventoryWriteService interface.
// =============================================================================
public class InventoryWriteService : IInventoryWriteService
{
    private readonly IRepository<Domain.Entities.Inventory> _inventoryRepo;
    private readonly IUnitOfWork _uow;

    public InventoryWriteService(
        IRepository<Domain.Entities.Inventory> inventoryRepo,
        IUnitOfWork uow)
    {
        _inventoryRepo = inventoryRepo;
        _uow           = uow;
    }

    public async Task SetStockQuantityAsync(long variantId, int stockQuantity, CancellationToken ct = default)
    {
        var inv = await _inventoryRepo.FirstOrDefaultAsync(i => i.VariantId == variantId, ct)
            ?? throw new KeyNotFoundException($"Inventory record not found for variant {variantId}.");

        inv.StockQuantity = stockQuantity;
        inv.UpdatedAt     = DateTime.UtcNow;
        _inventoryRepo.Update(inv);
        await _uow.SaveChangesAsync(ct);
    }
}
