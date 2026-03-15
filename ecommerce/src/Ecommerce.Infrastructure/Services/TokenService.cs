using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    /// <summary>
    /// Generates a short-lived signed JWT for API authentication.
    /// Expiry is read from JwtSettings:ExpiryMinutes (default 60).
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name,               user.Name),
            new Claim(ClaimTypes.Role,               user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var expireMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically random 64-byte refresh token (Base64url encoded).
    /// This is the RAW value returned to the client — it is never stored directly.
    /// The caller is responsible for hashing it before persistence.
    /// </summary>
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        // Base64url (no padding) — safe for HTTP headers and JSON without escaping
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Reads the principal from an expired JWT without validating its lifetime.
    /// Used during refresh to verify the token's signature and extract the user ID
    /// even after the token has expired.
    /// Throws <see cref="SecurityTokenException"/> if the token signature is invalid.
    /// </summary>
    public ClaimsPrincipal GetPrincipalFromExpiredToken(string accessToken)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured.");

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,  // ← intentionally skip expiry check
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(accessToken, tokenValidationParameters, out var validatedToken);

        // Double-check the algorithm — prevents "none" algorithm attacks
        if (validatedToken is not JwtSecurityToken jwt ||
            !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token algorithm.");
        }

        return principal;
    }
}