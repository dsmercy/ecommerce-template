using Ecommerce.Application.Modules.Coupons;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Modules.Coupons;

[ApiController]
[Route("api/coupons")]
[Produces("application/json")]
public class CouponsController : ControllerBase
{
    private readonly ICouponService _couponService;
    private readonly IValidator<CreateCouponRequest> _createValidator;

    public CouponsController(ICouponService couponService, IValidator<CreateCouponRequest> createValidator)
    {
        _couponService = couponService;
        _createValidator = createValidator;
    }

    /// <summary>List all coupons [Admin only]</summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _couponService.GetAllAsync(ct);
        return Ok(result);
    }

    /// <summary>Create a coupon [Admin only]</summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateCouponRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);
        var result = await _couponService.CreateAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Validate a coupon code against an order amount</summary>
    [HttpPost("validate")]
    [Authorize]
    public async Task<IActionResult> Validate([FromBody] ValidateCouponRequest request, CancellationToken ct)
    {
        var result = await _couponService.ValidateAsync(request, ct);
        return Ok(result);
    }
}
