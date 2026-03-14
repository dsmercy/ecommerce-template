using Ecommerce.Application.Common.Models;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Modules.Payments;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record CreatePaymentRequest(long OrderId, string PaymentMethod, string? TransactionId, decimal Amount);
public record UpdatePaymentStatusRequest(PaymentStatus Status, string? FailureReason = null);

public class PaymentResponse
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public decimal? Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime? PaidAt { get; set; }
}

// ── Validator ─────────────────────────────────────────────────────────────────

public class CreatePaymentValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

// ── Service ───────────────────────────────────────────────────────────────────

public interface IPaymentService
{
    Task<ApiResponse<PaymentResponse>> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken ct = default);
    Task<ApiResponse<PaymentResponse>> UpdateStatusAsync(long paymentId, UpdatePaymentStatusRequest request, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<PaymentResponse>>> GetOrderPaymentsAsync(long orderId, CancellationToken ct = default);
}

public class PaymentService : IPaymentService
{
    private readonly IRepository<Payment> _paymentRepo;
    private readonly IRepository<Order> _orderRepo;
    private readonly IRepository<OrderItem> _orderItemRepo;
    private readonly IInventoryProcedures _inventoryProcs;  // ← SP wrapper
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IRepository<Payment> paymentRepo,
        IRepository<Order> orderRepo,
        IRepository<OrderItem> orderItemRepo,
        IInventoryProcedures inventoryProcs,
        IUnitOfWork uow,
        ILogger<PaymentService> logger)
    {
        _paymentRepo    = paymentRepo;
        _orderRepo      = orderRepo;
        _orderItemRepo  = orderItemRepo;
        _inventoryProcs = inventoryProcs;
        _uow            = uow;
        _logger         = logger;
    }

    public async Task<ApiResponse<PaymentResponse>> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken ct = default)
    {
        var order = await _orderRepo.GetByIdAsync(request.OrderId, ct);
        if (order is null) return ApiResponse<PaymentResponse>.Fail("Order not found.");

        var payment = new Payment
        {
            OrderId       = request.OrderId,
            PaymentMethod = request.PaymentMethod,
            TransactionId = request.TransactionId,
            Amount        = request.Amount,
            Status        = PaymentStatus.PENDING
        };

        await _paymentRepo.AddAsync(payment, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Payment {PaymentId} created for Order {OrderId}", payment.Id, request.OrderId);
        return ApiResponse<PaymentResponse>.Ok(MapResponse(payment), "Payment created.");
    }

    public async Task<ApiResponse<PaymentResponse>> UpdateStatusAsync(long paymentId, UpdatePaymentStatusRequest request, CancellationToken ct = default)
    {
        var payment = await _paymentRepo.GetByIdAsync(paymentId, ct);
        if (payment is null) return ApiResponse<PaymentResponse>.Fail("Payment not found.");

        payment.Status = request.Status;
        if (request.Status == PaymentStatus.COMPLETED) payment.PaidAt = DateTime.UtcNow;
        if (request.Status == PaymentStatus.FAILED)    payment.FailureReason = request.FailureReason;

        _paymentRepo.Update(payment);

        if (request.Status == PaymentStatus.COMPLETED)
        {
            // ── Advance order to PAID ─────────────────────────────────────────
            var order = await _orderRepo.GetByIdAsync(payment.OrderId, ct);
            if (order is not null) { order.Status = OrderStatus.PAID; _orderRepo.Update(order); }

            // ── DEDUCT STOCK via sp_deduct_stock ──────────────────────────────
            // Permanently decrements both stock_quantity AND reserved_quantity.
            // The SP runs inside its own statement — no overselling possible
            // because stock was already reserved via sp_reserve_stock at order time.
            var orderItems = await _orderItemRepo
                .FindAsync(oi => oi.OrderId == payment.OrderId, ct);

            foreach (var item in orderItems.Where(i => i.VariantId.HasValue && i.Quantity.HasValue))
            {
                await _inventoryProcs.DeductStockAsync(
                    item.VariantId!.Value, item.Quantity!.Value, ct);
            }

            _logger.LogInformation(
                "Payment {PaymentId} COMPLETED — stock deducted for Order {OrderId}",
                paymentId, payment.OrderId);
        }

        if (request.Status == PaymentStatus.FAILED)
        {
            // ── RELEASE RESERVATION via sp_release_reservation on FAILED ──────
            // Payment gateway rejected the charge — undo the reservation so
            // the customer (or another buyer) can try again.
            var orderItems = await _orderItemRepo
                .FindAsync(oi => oi.OrderId == payment.OrderId, ct);

            foreach (var item in orderItems.Where(i => i.VariantId.HasValue && i.Quantity.HasValue))
            {
                await _inventoryProcs.ReleaseReservationAsync(
                    item.VariantId!.Value, item.Quantity!.Value, ct);
            }

            _logger.LogWarning(
                "Payment {PaymentId} FAILED — reservations released. Reason: {Reason}",
                paymentId, request.FailureReason);
        }

        await _uow.SaveChangesAsync(ct);
        return ApiResponse<PaymentResponse>.Ok(MapResponse(payment), "Payment status updated.");
    }

    public async Task<ApiResponse<IEnumerable<PaymentResponse>>> GetOrderPaymentsAsync(long orderId, CancellationToken ct = default)
    {
        var payments = await _paymentRepo.FindAsync(p => p.OrderId == orderId, ct);
        return ApiResponse<IEnumerable<PaymentResponse>>.Ok(payments.Select(MapResponse));
    }

    private static PaymentResponse MapResponse(Payment p) => new()
    {
        Id            = p.Id,
        OrderId       = p.OrderId,
        PaymentMethod = p.PaymentMethod,
        TransactionId = p.TransactionId,
        Amount        = p.Amount,
        Status        = p.Status.ToString(),
        FailureReason = p.FailureReason,
        PaidAt        = p.PaidAt
    };
}
