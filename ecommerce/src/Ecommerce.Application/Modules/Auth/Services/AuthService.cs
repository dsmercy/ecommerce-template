using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Modules.Auth.DTOs;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Modules.Auth.Services;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepo;
    private readonly IUnitOfWork _uow;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IRepository<User> userRepo,
        IUnitOfWork uow,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _userRepo = userRepo;
        _uow = uow;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
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

        var token = _tokenService.GenerateToken(user);
        return ApiResponse<AuthResponse>.Ok(new AuthResponse(user.Id, user.Name, user.Email, user.Role.ToString(), token), "Registration successful.");
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userRepo.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ApiResponse<AuthResponse>.Fail("Invalid email or password.");

        _logger.LogInformation("User logged in: {Email}", user.Email);

        var token = _tokenService.GenerateToken(user);
        return ApiResponse<AuthResponse>.Ok(new AuthResponse(user.Id, user.Name, user.Email, user.Role.ToString(), token));
    }
}
