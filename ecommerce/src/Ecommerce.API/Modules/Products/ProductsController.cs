using Ecommerce.Application.Modules.Products.DTOs;
using Ecommerce.Application.Modules.Products.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Modules.Products;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<CreateVariantRequest> _variantValidator;

    public ProductsController(
        IProductService productService,
        IValidator<CreateProductRequest> createValidator,
        IValidator<CreateVariantRequest> variantValidator)
    {
        _productService = productService;
        _createValidator = createValidator;
        _variantValidator = variantValidator;
    }

    /// <summary>Get paginated product list with filters</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ProductFilterParams filter, CancellationToken ct)
    {
        var result = await _productService.GetAllAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>Get a single product with all variants and images</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _productService.GetByIdAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Create a new product [Admin only]</summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);
        var result = await _productService.CreateAsync(request, ct);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>Update an existing product [Admin only]</summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var result = await _productService.UpdateAsync(id, request, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Soft-delete a product [Admin only]</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _productService.DeleteAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Add a variant (SKU/color/size) to a product [Admin only]</summary>
    [HttpPost("{id:long}/variants")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AddVariant(long id, [FromBody] CreateVariantRequest request, CancellationToken ct)
    {
        await _variantValidator.ValidateAndThrowAsync(request, ct);
        var result = await _productService.AddVariantAsync(id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Upload an image for a product [Admin only]</summary>
    [HttpPost("{id:long}/images")]
    [Authorize(Roles = "ADMIN")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> UploadImage(long id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest("Only JPEG, PNG, and WebP images are allowed.");

        using var stream = file.OpenReadStream();
        var result = await _productService.UploadImageAsync(id, stream, file.FileName, file.ContentType, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
