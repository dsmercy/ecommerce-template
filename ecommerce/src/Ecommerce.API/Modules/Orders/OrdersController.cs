using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Modules.Orders;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Modules.Orders;

[ApiController]
[Route("api/orders")]
[Authorize]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<CreateOrderRequest> _createValidator;

    public OrdersController(
        IOrderService orderService,
        ICurrentUserService currentUser,
        IValidator<CreateOrderRequest> createValidator)
    {
        _orderService    = orderService;
        _currentUser     = currentUser;
        _createValidator = createValidator;
    }

    /// <summary>Create an order from the current cart (reserves stock via sp_reserve_stock)</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);
        var result = await _orderService.CreateOrderAsync(_currentUser.UserId!.Value, request, ct);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result)
            : BadRequest(result);
    }

    /// <summary>Get a specific order by ID (enriched from v_order_summary)</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _orderService.GetOrderAsync(id, _currentUser.UserId!.Value, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Get current user's order history (reads from v_order_summary view)</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _orderService.GetUserOrdersAsync(_currentUser.UserId!.Value, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Admin: all orders — backed entirely by v_order_summary view.
    /// No table joins in application code — user info + payment status come from the view.
    /// </summary>
    [HttpGet("admin/summary")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAllSummaries(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _orderService.GetAllOrderSummariesAsync(page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Update order status [Admin].
    /// CANCELLED → triggers sp_release_reservation for all order items.
    /// </summary>
    [HttpPatch("{id:long}/status")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        var result = await _orderService.UpdateStatusAsync(id, request, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
