using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(u => u.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        builder.Property(u => u.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(u => u.Role).HasColumnName("role")
            .HasConversion<string>()
            .HasDefaultValue(UserRole.CUSTOMER);

        // ── Refresh token columns ─────────────────────────────────────────────
        // refresh_token_hash stores the BCrypt hash of the raw token.
        // 255 chars covers the BCrypt output length (60 chars) with headroom.
        builder.Property(u => u.RefreshTokenHash)
            .HasColumnName("refresh_token_hash")
            .HasMaxLength(255);

        builder.Property(u => u.RefreshTokenExpiry)
            .HasColumnName("refresh_token_expiry");

        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");

        builder.HasMany(u => u.Addresses)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Cart)
            .WithOne(c => c.User)
            .HasForeignKey<Cart>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}