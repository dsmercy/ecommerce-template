using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Modules.Auth.DTOs;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Ecommerce.Application.Modules.Auth.Services;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ApiResponse<RefreshResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> RevokeAsync(long userId, RevokeRequest request, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepo;
    private readonly IUnitOfWork _uow;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IRepository<User> userRepo,
        IUnitOfWork uow,
        ITokenService tokenService,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _userRepo = userRepo;
        _uow = uow;
        _tokenService = tokenService;
        _config = config;
        _logger = logger;
    }

    // ── Register ─────────────────────────────────────────────────────────────

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(
        RegisterRequest request, CancellationToken ct = default)
    {
        var exists = await _userRepo.AnyAsync(u => u.Email == request.Email, ct);
        if (exists)
            return ApiResponse<AuthResponse>.Fail("Email already registered.");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Phone = request.Phone
        };

        await _userRepo.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("New user registered: {Email}", user.Email);

        var (accessToken, rawRefresh, expiry) = IssueTokenPair(user);
        await PersistRefreshTokenAsync(user, rawRefresh, expiry, ct);

        return ApiResponse<AuthResponse>.Ok(
            BuildAuthResponse(user, accessToken, rawRefresh, expiry),
            "Registration successful.");
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    public async Task<ApiResponse<AuthResponse>> LoginAsync(
        LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userRepo.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ApiResponse<AuthResponse>.Fail("Invalid email or password.");

        _logger.LogInformation("User logged in: {Email}", user.Email);

        var (accessToken, rawRefresh, expiry) = IssueTokenPair(user);
        await PersistRefreshTokenAsync(user, rawRefresh, expiry, ct);

        return ApiResponse<AuthResponse>.Ok(
            BuildAuthResponse(user, accessToken, rawRefresh, expiry));
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the expired access token + refresh token pair and issues a
    /// new pair (rotation). The old refresh token is invalidated immediately.
    ///
    /// Rotation means every refresh consumes the token — a stolen refresh token
    /// can only be used once before the legitimate client's next refresh
    /// invalidates it and the attacker's attempt returns 401.
    /// </summary>
    public async Task<ApiResponse<RefreshResponse>> RefreshAsync(
        RefreshRequest request, CancellationToken ct = default)
    {
        // 1. Validate the access token's signature (lifetime check is skipped)
        //    This confirms the token was genuinely issued by this server.
        ClaimsPrincipal principal;
        try
        {
            principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("Refresh rejected — invalid access token signature: {Message}", ex.Message);
            return ApiResponse<RefreshResponse>.Fail("Invalid token.");
        }

        // 2. Resolve the user from the access token's sub claim
        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? principal.FindFirst("sub")?.Value;

        if (!long.TryParse(userIdClaim, out var userId))
            return ApiResponse<RefreshResponse>.Fail("Invalid token.");

        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null)
            return ApiResponse<RefreshResponse>.Fail("Invalid token.");

        // 3. Check the refresh token is not expired
        if (user.RefreshTokenExpiry is null || user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            _logger.LogWarning(
                "Refresh rejected — token expired | UserId={UserId}", userId);
            return ApiResponse<RefreshResponse>.Fail("Refresh token expired. Please log in again.");
        }

        // 4. Verify the raw refresh token against the stored hash
        if (user.RefreshTokenHash is null ||
            !BCrypt.Net.BCrypt.Verify(request.RefreshToken, user.RefreshTokenHash))
        {
            // A hash mismatch after the token is still within its expiry window
            // is a strong signal of token theft — log at Warning for alerting.
            _logger.LogWarning(
                "Refresh rejected — hash mismatch (possible token theft) | UserId={UserId}", userId);
            return ApiResponse<RefreshResponse>.Fail("Invalid refresh token.");
        }

        // 5. Issue a new pair (rotation — old token is replaced atomically)
        var (newAccessToken, newRawRefresh, newExpiry) = IssueTokenPair(user);
        await PersistRefreshTokenAsync(user, newRawRefresh, newExpiry, ct);

        _logger.LogInformation("Tokens rotated for UserId={UserId}", userId);

        return ApiResponse<RefreshResponse>.Ok(new RefreshResponse(
            newAccessToken, newRawRefresh, newExpiry));
    }

    // ── Revoke ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Clears the stored refresh token hash for the user (logout).
    /// The access token remains valid until its natural expiry — callers
    /// should discard it client-side immediately after revoking.
    /// </summary>
    public async Task<ApiResponse<bool>> RevokeAsync(
        long userId, RevokeRequest request, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null)
            return ApiResponse<bool>.Fail("User not found.");

        // Verify the token belongs to this user before clearing it.
        // Prevents one authenticated user from revoking another user's session
        // if they somehow obtain a different user's refresh token.
        if (user.RefreshTokenHash is null ||
            !BCrypt.Net.BCrypt.Verify(request.RefreshToken, user.RefreshTokenHash))
            return ApiResponse<bool>.Fail("Invalid refresh token.");

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiry = null;
        _userRepo.Update(user);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Refresh token revoked for UserId={UserId}", userId);
        return ApiResponse<bool>.Ok(true, "Logged out successfully.");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private (string AccessToken, string RawRefresh, DateTime Expiry) IssueTokenPair(User user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var rawRefresh = _tokenService.GenerateRefreshToken();
        var expiry = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays);
        return (accessToken, rawRefresh, expiry);
    }

    private async Task PersistRefreshTokenAsync(
        User user, string rawRefreshToken, DateTime expiry, CancellationToken ct)
    {
        // Hash before storing — a plaintext refresh token in the DB is as bad
        // as a plaintext password. BCrypt work factor 11 is intentionally lower
        // than password hashing (12+) because refresh is on the hot path.
        user.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(rawRefreshToken, workFactor: 11);
        user.RefreshTokenExpiry = expiry;
        _userRepo.Update(user);
        await _uow.SaveChangesAsync(ct);
    }

    private static AuthResponse BuildAuthResponse(
        User user, string accessToken, string rawRefresh, DateTime expiry) =>
        new(user.Id, user.Name, user.Email, user.Role.ToString(),
            accessToken, rawRefresh, expiry);

    private int RefreshTokenLifetimeDays =>
        int.TryParse(_config["JwtSettings:RefreshTokenLifetimeDays"], out var d) ? d : 7;
}