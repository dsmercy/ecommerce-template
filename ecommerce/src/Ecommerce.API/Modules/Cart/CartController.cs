using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Modules.Carts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Modules.Cart;

[ApiController]
[Route("api/cart")]
[Authorize]
[Produces("application/json")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<AddToCartRequest> _addValidator;
    private readonly IValidator<UpdateCartItemRequest> _updateValidator;

    public CartController(
        ICartService cartService,
        ICurrentUserService currentUser,
        IValidator<AddToCartRequest> addValidator,
        IValidator<UpdateCartItemRequest> updateValidator)
    {
        _cartService = cartService;
        _currentUser = currentUser;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Get the current user's cart</summary>
    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        var result = await _cartService.GetCartAsync(_currentUser.UserId!.Value, ct);
        return Ok(result);
    }

    /// <summary>Add an item to the cart</summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request, CancellationToken ct)
    {
        await _addValidator.ValidateAndThrowAsync(request, ct);
        var result = await _cartService.AddItemAsync(_currentUser.UserId!.Value, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Update quantity of a cart item</summary>
    [HttpPut("items/{itemId:long}")]
    public async Task<IActionResult> UpdateItem(long itemId, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);
        var result = await _cartService.UpdateItemAsync(_currentUser.UserId!.Value, itemId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Remove an item from the cart</summary>
    [HttpDelete("items/{itemId:long}")]
    public async Task<IActionResult> RemoveItem(long itemId, CancellationToken ct)
    {
        var result = await _cartService.RemoveItemAsync(_currentUser.UserId!.Value, itemId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Clear all items from the cart</summary>
    [HttpDelete]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
    {
        var result = await _cartService.ClearCartAsync(_currentUser.UserId!.Value, ct);
        return Ok(result);
    }
}
