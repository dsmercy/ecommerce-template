using Ecommerce.Application.Modules.Inventories;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Modules.Inventory;

[ApiController]
[Route("api/inventory")]
[Produces("application/json")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IValidator<UpdateStockRequest> _updateValidator;

    public InventoryController(IInventoryService inventoryService, IValidator<UpdateStockRequest> updateValidator)
    {
        _inventoryService = inventoryService;
        _updateValidator  = updateValidator;
    }

    /// <summary>
    /// Get inventory for a single variant (reads from v_inventory_available view).
    /// Response includes available_quantity = stock - reserved.
    /// </summary>
    [HttpGet("variant/{variantId:long}")]
    public async Task<IActionResult> GetByVariant(long variantId, CancellationToken ct)
    {
        var result = await _inventoryService.GetByVariantAsync(variantId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Get inventory for ALL variants of a product (reads from v_inventory_available view).
    /// Useful for admin stock overview or product detail pages.
    /// </summary>
    [HttpGet("product/{productId:long}")]
    public async Task<IActionResult> GetByProduct(long productId, CancellationToken ct)
    {
        var result = await _inventoryService.GetByProductAsync(productId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Admin restock: directly set stock_quantity for a variant.
    /// Does NOT go through a stored procedure — this is a manual override
    /// (e.g. after a physical warehouse count), not a transactional reserve/deduct.
    /// </summary>
    [HttpPut("variant/{variantId:long}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateStock(long variantId, [FromBody] UpdateStockRequest request, CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);
        var result = await _inventoryService.UpdateStockAsync(variantId, request, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
