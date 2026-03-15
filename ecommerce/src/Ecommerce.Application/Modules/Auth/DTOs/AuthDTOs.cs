namespace Ecommerce.Application.Modules.Auth.DTOs;

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string? Phone
);

public record LoginRequest(
    string Email,
    string Password
);

public record RefreshRequest(
    string AccessToken,
    string RefreshToken
);

public record RevokeRequest(
    string RefreshToken
);

public record AuthResponse(
    long UserId,
    string Name,
    string Email,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiry
);

public record RefreshResponse(
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiry
);