using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Modules.Reviews;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Modules.Reviews;

[ApiController]
[Route("api/reviews")]
[Produces("application/json")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<CreateReviewRequest> _validator;

    public ReviewsController(IReviewService reviewService, ICurrentUserService currentUser, IValidator<CreateReviewRequest> validator)
    {
        _reviewService = reviewService;
        _currentUser = currentUser;
        _validator = validator;
    }

    /// <summary>Get reviews for a product</summary>
    [HttpGet("product/{productId:long}")]
    public async Task<IActionResult> GetProductReviews(long productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _reviewService.GetProductReviewsAsync(productId, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Submit a product review [Authenticated]</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest request, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);
        var result = await _reviewService.AddReviewAsync(_currentUser.UserId!.Value, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
