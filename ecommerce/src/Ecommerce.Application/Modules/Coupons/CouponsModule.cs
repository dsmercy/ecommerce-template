using Ecommerce.Application.Common.Models;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Interfaces;
using FluentValidation;

namespace Ecommerce.Application.Modules.Coupons;

public record CreateCouponRequest(
    string Code, DiscountType DiscountType, decimal DiscountValue,
    decimal? MinOrderAmount, decimal? MaxDiscount, DateTime? ExpiryDate, int? UsageLimit);

public record ValidateCouponRequest(string Code, decimal OrderAmount);

public class CouponResponse
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class ValidateCouponResponse
{
    public bool IsValid { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Message { get; set; }
}

public class CreateCouponValidator : AbstractValidator<CreateCouponRequest>
{
    public CreateCouponValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.DiscountValue).LessThanOrEqualTo(100)
            .When(x => x.DiscountType == DiscountType.PERCENTAGE);
    }
}

public interface ICouponService
{
    Task<ApiResponse<CouponResponse>> CreateAsync(CreateCouponRequest request, CancellationToken ct = default);
    Task<ApiResponse<ValidateCouponResponse>> ValidateAsync(ValidateCouponRequest request, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<CouponResponse>>> GetAllAsync(CancellationToken ct = default);
}

public class CouponService : ICouponService
{
    private readonly IRepository<Coupon> _couponRepo;
    private readonly IUnitOfWork _uow;

    public CouponService(IRepository<Coupon> couponRepo, IUnitOfWork uow) { _couponRepo = couponRepo; _uow = uow; }

    public async Task<ApiResponse<CouponResponse>> CreateAsync(CreateCouponRequest request, CancellationToken ct = default)
    {
        var exists = await _couponRepo.AnyAsync(c => c.Code == request.Code, ct);
        if (exists) return ApiResponse<CouponResponse>.Fail("Coupon code already exists.");

        var coupon = new Coupon
        {
            Code = request.Code, DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue, MinOrderAmount = request.MinOrderAmount,
            MaxDiscount = request.MaxDiscount, ExpiryDate = request.ExpiryDate, UsageLimit = request.UsageLimit
        };
        await _couponRepo.AddAsync(coupon, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<CouponResponse>.Ok(MapResponse(coupon), "Coupon created.");
    }

    public async Task<ApiResponse<ValidateCouponResponse>> ValidateAsync(ValidateCouponRequest request, CancellationToken ct = default)
    {
        var coupon = await _couponRepo.FirstOrDefaultAsync(c => c.Code == request.Code, ct);
        if (coupon is null)
            return ApiResponse<ValidateCouponResponse>.Ok(new ValidateCouponResponse { IsValid = false, Message = "Coupon not found." });

        if (!coupon.IsValid(request.OrderAmount))
            return ApiResponse<ValidateCouponResponse>.Ok(new ValidateCouponResponse { IsValid = false, Message = "Coupon is invalid or expired." });

        return ApiResponse<ValidateCouponResponse>.Ok(new ValidateCouponResponse
        {
            IsValid = true,
            DiscountAmount = coupon.CalculateDiscount(request.OrderAmount),
            Message = "Coupon is valid."
        });
    }

    public async Task<ApiResponse<IEnumerable<CouponResponse>>> GetAllAsync(CancellationToken ct = default)
    {
        var coupons = await _couponRepo.GetAllAsync(ct);
        return ApiResponse<IEnumerable<CouponResponse>>.Ok(coupons.Select(MapResponse));
    }

    private static CouponResponse MapResponse(Coupon c) => new()
    {
        Id = c.Id, Code = c.Code, DiscountType = c.DiscountType.ToString(),
        DiscountValue = c.DiscountValue, MinOrderAmount = c.MinOrderAmount,
        MaxDiscount = c.MaxDiscount, ExpiryDate = c.ExpiryDate
    };
}
