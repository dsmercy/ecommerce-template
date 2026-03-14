using Ecommerce.Application.Common.Models;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Modules.Reviews;

public record CreateReviewRequest(long ProductId, int Rating, string? Comment);

public class ReviewResponse
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string? UserName { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(2000).When(x => x.Comment is not null);
    }
}

public interface IReviewService
{
    Task<ApiResponse<ReviewResponse>> AddReviewAsync(long userId, CreateReviewRequest request, CancellationToken ct = default);
    Task<ApiResponse<PagedResult<ReviewResponse>>> GetProductReviewsAsync(long productId, int page, int pageSize, CancellationToken ct = default);
}

public class ReviewService : IReviewService
{
    private readonly IRepository<Review> _reviewRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IUnitOfWork _uow;

    public ReviewService(IRepository<Review> reviewRepo, IRepository<Product> productRepo, IUnitOfWork uow)
    {
        _reviewRepo = reviewRepo;
        _productRepo = productRepo;
        _uow = uow;
    }

    public async Task<ApiResponse<ReviewResponse>> AddReviewAsync(long userId, CreateReviewRequest request, CancellationToken ct = default)
    {
        var product = await _productRepo.GetByIdAsync(request.ProductId, ct);
        if (product is null) return ApiResponse<ReviewResponse>.Fail("Product not found.");

        var alreadyReviewed = await _reviewRepo.AnyAsync(r => r.UserId == userId && r.ProductId == request.ProductId, ct);
        if (alreadyReviewed) return ApiResponse<ReviewResponse>.Fail("You have already reviewed this product.");

        var review = new Review
        {
            UserId = userId,
            ProductId = request.ProductId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        await _reviewRepo.AddAsync(review, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<ReviewResponse>.Ok(new ReviewResponse
        {
            Id = review.Id,
            UserId = review.UserId,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        }, "Review added.");
    }

    public async Task<ApiResponse<PagedResult<ReviewResponse>>> GetProductReviewsAsync(long productId, int page, int pageSize, CancellationToken ct = default)
    {
        var total = await _reviewRepo.CountAsync(r => r.ProductId == productId, ct);
        var reviews = await _reviewRepo.Query()
            .Include(r => r.User)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReviewResponse
            {
                Id = r.Id, UserId = r.UserId, UserName = r.User.Name,
                Rating = r.Rating, Comment = r.Comment, CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return ApiResponse<PagedResult<ReviewResponse>>.Ok(new PagedResult<ReviewResponse>
        {
            Items = reviews, TotalCount = total, Page = page, PageSize = pageSize
        });
    }
}
