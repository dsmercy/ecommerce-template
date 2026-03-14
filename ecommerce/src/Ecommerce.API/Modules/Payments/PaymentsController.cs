using Ecommerce.Application.Modules.Payments;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Modules.Payments;

[ApiController]
[Route("api/payments")]
[Authorize]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IValidator<CreatePaymentRequest> _createValidator;

    public PaymentsController(IPaymentService paymentService, IValidator<CreatePaymentRequest> createValidator)
    {
        _paymentService = paymentService;
        _createValidator = createValidator;
    }

    /// <summary>Record a new payment for an order</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);
        var result = await _paymentService.CreatePaymentAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Update payment status [Admin only]</summary>
    [HttpPatch("{id:long}/status")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdatePaymentStatusRequest request, CancellationToken ct)
    {
        var result = await _paymentService.UpdateStatusAsync(id, request, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Get all payments for an order</summary>
    [HttpGet("order/{orderId:long}")]
    public async Task<IActionResult> GetByOrder(long orderId, CancellationToken ct)
    {
        var result = await _paymentService.GetOrderPaymentsAsync(orderId, ct);
        return Ok(result);
    }
}
