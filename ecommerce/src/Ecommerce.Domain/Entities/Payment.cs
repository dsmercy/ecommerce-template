using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities;

public class Payment : BaseEntity
{
    public long OrderId { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public decimal? Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;
    public string? FailureReason { get; set; }
    public DateTime? PaidAt { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
}
