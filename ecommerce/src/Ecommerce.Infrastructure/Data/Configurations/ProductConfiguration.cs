using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(p => p.CategoryId).HasColumnName("category_id");
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(p => p.Slug).HasColumnName("slug").HasMaxLength(255);
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => p.Name);
        builder.Property(p => p.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(p => p.Brand).HasColumnName("brand").HasMaxLength(150);
        builder.Property(p => p.BasePrice).HasColumnName("base_price").HasPrecision(10, 2);
        builder.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Images)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(p => p.ProductId).HasColumnName("product_id");
        builder.Property(p => p.ImageUrl).HasColumnName("image_url").HasMaxLength(500).IsRequired();
        builder.Property(p => p.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(v => v.ProductId).HasColumnName("product_id");
        builder.Property(v => v.Sku).HasColumnName("sku").HasMaxLength(100).IsRequired();
        builder.HasIndex(v => v.Sku).IsUnique();
        builder.HasIndex(v => v.ProductId);
        builder.Property(v => v.Color).HasColumnName("color").HasMaxLength(50);
        builder.Property(v => v.Size).HasColumnName("size").HasMaxLength(50);
        builder.Property(v => v.Price).HasColumnName("price").HasPrecision(10, 2);
        builder.Property(v => v.CreatedAt).HasColumnName("created_at");

        builder.HasOne(v => v.Inventory)
            .WithOne(i => i.Variant)
            .HasForeignKey<Inventory>(i => i.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("inventory");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(i => i.VariantId).HasColumnName("variant_id");
        builder.HasIndex(i => i.VariantId);
        builder.Property(i => i.StockQuantity).HasColumnName("stock_quantity").HasDefaultValue(0);
        builder.Property(i => i.ReservedQuantity).HasColumnName("reserved_quantity").HasDefaultValue(0);
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(i => i.AvailableQuantity);
        builder.Ignore(i => i.CreatedAt);
    }
}
