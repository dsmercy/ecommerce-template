using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Common.Models;
using FluentValidation;

namespace Ecommerce.Application.Modules.Inventories;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record UpdateStockRequest(int StockQuantity);

public class InventoryResponse
{
    public long Id { get; set; }
    public long VariantId { get; set; }
    public string? Sku { get; set; }
    public long ProductId { get; set; }
    public int StockQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ── Validator ─────────────────────────────────────────────────────────────────

public class UpdateStockValidator : AbstractValidator<UpdateStockRequest>
{
    public UpdateStockValidator() => RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
}

// ── Service Interface ─────────────────────────────────────────────────────────

public interface IInventoryService
{
    Task<ApiResponse<InventoryResponse>> GetByVariantAsync(long variantId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<InventoryResponse>>> GetByProductAsync(long productId, CancellationToken ct = default);
    Task<ApiResponse<InventoryResponse>> UpdateStockAsync(long variantId, UpdateStockRequest request, CancellationToken ct = default);
}

// ── Service Implementation ────────────────────────────────────────────────────
//
// ALL Infrastructure types (AppDbContext, IRepository<T>, IUnitOfWork) are
// accessed ONLY through interfaces defined here in Application.Common.Interfaces:
//
//   IInventoryQueryService  → reads v_inventory_available view
//   IInventoryWriteService  → writes inventory table
//
// Both interfaces are DEFINED in Application, IMPLEMENTED in Infrastructure.
// This class has zero project-level dependency on Ecommerce.Infrastructure.

public class InventoryService : IInventoryService
{
    private readonly IInventoryQueryService _inventoryQuery;
    private readonly IInventoryWriteService _inventoryWrite;

    public InventoryService(
        IInventoryQueryService inventoryQuery,
        IInventoryWriteService inventoryWrite)
    {
        _inventoryQuery = inventoryQuery;
        _inventoryWrite = inventoryWrite;
    }

    public async Task<ApiResponse<InventoryResponse>> GetByVariantAsync(
        long variantId, CancellationToken ct = default)
    {
        var row = await _inventoryQuery.GetByVariantAsync(variantId, ct);
        return row is null
            ? ApiResponse<InventoryResponse>.Fail("Inventory record not found.")
            : ApiResponse<InventoryResponse>.Ok(Map(row));
    }

    public async Task<ApiResponse<IEnumerable<InventoryResponse>>> GetByProductAsync(
        long productId, CancellationToken ct = default)
    {
        var rows = await _inventoryQuery.GetByProductAsync(productId, ct);
        return ApiResponse<IEnumerable<InventoryResponse>>.Ok(rows.Select(Map));
    }

    public async Task<ApiResponse<InventoryResponse>> UpdateStockAsync(
        long variantId, UpdateStockRequest request, CancellationToken ct = default)
    {
        var exists = await _inventoryQuery.GetByVariantAsync(variantId, ct);
        if (exists is null)
            return ApiResponse<InventoryResponse>.Fail("Inventory record not found.");

        // Delegate the write to Infrastructure via interface — no IRepository here
        await _inventoryWrite.SetStockQuantityAsync(variantId, request.StockQuantity, ct);

        // Re-read from view to get fresh available_quantity
        var updated = await _inventoryQuery.GetByVariantAsync(variantId, ct);
        return ApiResponse<InventoryResponse>.Ok(Map(updated!), "Stock updated.");
    }

    private static InventoryResponse Map(InventoryAvailableRow r) => new()
    {
        Id                = r.Id,
        VariantId         = r.VariantId,
        Sku               = r.Sku,
        ProductId         = r.ProductId,
        StockQuantity     = r.StockQuantity,
        ReservedQuantity  = r.ReservedQuantity,
        AvailableQuantity = r.AvailableQuantity,
        UpdatedAt         = r.UpdatedAt
    };
}
