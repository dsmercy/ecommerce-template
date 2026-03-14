using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Modules.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Modules.Search;

[ApiController]
[Route("api/search")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly IProductSearchService _searchService;

    public SearchController(IProductSearchService searchService)
        => _searchService = searchService;

    /// <summary>Full-text product search via Meilisearch</summary>
    [HttpGet("products")]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] ProductSearchFilter filter, CancellationToken ct)
    {
        var result = await _searchService.SearchAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Sync all active products to Meilisearch [Admin only].
    /// Run this once after first deploy, after seeding, or after schema changes.
    /// </summary>
    [HttpPost("sync")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> SyncAll(CancellationToken ct)
    {
        var result = await _searchService.SyncAllProductsAsync(ct);
        return Ok(result);
    }
}