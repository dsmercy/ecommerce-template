using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities;

public class User : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserRole Role { get; set; } = UserRole.CUSTOMER;

    // ── Refresh token ─────────────────────────────────────────────────────────
    // The raw token is never persisted. Only a BCrypt hash is stored so that a
    // compromised database does not expose valid refresh tokens.
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    // Navigation
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public Cart? Cart { get; set; }
}