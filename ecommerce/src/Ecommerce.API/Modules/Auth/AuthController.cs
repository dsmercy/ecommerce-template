using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Modules.Auth.DTOs;
using Ecommerce.Application.Modules.Auth.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Modules.Auth;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshRequest> _refreshValidator;
    private readonly IValidator<RevokeRequest> _revokeValidator;

    public AuthController(
        IAuthService authService,
        ICurrentUserService currentUser,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshRequest> refreshValidator,
        IValidator<RevokeRequest> revokeValidator)
    {
        _authService = authService;
        _currentUser = currentUser;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
        _revokeValidator = revokeValidator;
    }

    /// <summary>Register a new user account</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        await _registerValidator.ValidateAndThrowAsync(request, ct);
        var result = await _authService.RegisterAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Login and receive an access token + refresh token</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        await _loginValidator.ValidateAndThrowAsync(request, ct);
        var result = await _authService.LoginAsync(request, ct);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>
    /// Exchange an expired access token + valid refresh token for a new pair.
    /// The supplied refresh token is immediately invalidated (rotation).
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        await _refreshValidator.ValidateAndThrowAsync(request, ct);
        var result = await _authService.RefreshAsync(request, ct);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>
    /// Revoke the current refresh token (logout).
    /// Requires a valid access token — the refresh token is verified against
    /// the authenticated user's record before being cleared.
    /// </summary>
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest request, CancellationToken ct)
    {
        await _revokeValidator.ValidateAndThrowAsync(request, ct);
        var result = await _authService.RevokeAsync(_currentUser.UserId!.Value, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}