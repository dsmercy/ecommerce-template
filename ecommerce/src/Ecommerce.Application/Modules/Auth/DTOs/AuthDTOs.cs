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

public record AuthResponse(
    long UserId,
    string Name,
    string Email,
    string Role,
    string Token
);
