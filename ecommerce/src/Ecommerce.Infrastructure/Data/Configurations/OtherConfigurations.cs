using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(c => c.Slug).HasColumnName("slug").HasMaxLength(150);
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.Property(c => c.ParentId).HasColumnName("parent_id");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.HasOne(c => c.Parent).WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(a => a.UserId).HasColumnName("user_id");
        builder.Property(a => a.AddressLine1).HasColumnName("address_line1").HasMaxLength(255).IsRequired();
        builder.Property(a => a.AddressLine2).HasColumnName("address_line2").HasMaxLength(255);
        builder.Property(a => a.City).HasColumnName("city").HasMaxLength(100);
        builder.Property(a => a.State).HasColumnName("state").HasMaxLength(100);
        builder.Property(a => a.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
        builder.Property(a => a.Country).HasColumnName("country").HasMaxLength(100);
        builder.Property(a => a.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
    }
}

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("carts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(c => c.UserId).HasColumnName("user_id");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.HasMany(c => c.Items).WithOne(i => i.Cart)
            .HasForeignKey(i => i.CartId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("cart_items");
        builder.HasKey(ci => ci.Id);
        builder.Property(ci => ci.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(ci => ci.CartId).HasColumnName("cart_id");
        builder.Property(ci => ci.VariantId).HasColumnName("variant_id");
        builder.Property(ci => ci.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(ci => ci.AddedAt).HasColumnName("added_at");
        builder.Ignore(ci => ci.CreatedAt);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(o => o.UserId).HasColumnName("user_id");
        builder.Property(o => o.CouponId).HasColumnName("coupon_id");
        builder.Property(o => o.Status).HasColumnName("status")
            .HasConversion<string>().HasDefaultValue(OrderStatus.PENDING);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.UserId);
        builder.Property(o => o.SubtotalAmount).HasColumnName("subtotal_amount").HasPrecision(12, 2);
        builder.Property(o => o.DiscountAmount).HasColumnName("discount_amount").HasPrecision(12, 2);
        builder.Property(o => o.TotalAmount).HasColumnName("total_amount").HasPrecision(12, 2);
        builder.Property(o => o.ShippingAddressId).HasColumnName("shipping_address_id");
        builder.Property(o => o.Notes).HasColumnName("notes").HasColumnType("text");
        builder.Property(o => o.CreatedAt).HasColumnName("created_at");

        builder.HasMany(o => o.Items).WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(o => o.Payments).WithOne(p => p.Order)
            .HasForeignKey(p => p.OrderId);
        builder.HasOne(o => o.ShippingAddress).WithMany(a => a.Orders)
            .HasForeignKey(o => o.ShippingAddressId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(o => o.Coupon).WithMany(c => c.Orders)
            .HasForeignKey(o => o.CouponId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(oi => oi.Id);
        builder.Property(oi => oi.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(oi => oi.OrderId).HasColumnName("order_id");
        builder.Property(oi => oi.VariantId).HasColumnName("variant_id");
        builder.Property(oi => oi.Sku).HasColumnName("sku").HasMaxLength(100);
        builder.Property(oi => oi.ProductName).HasColumnName("product_name").HasMaxLength(255);
        builder.Property(oi => oi.Color).HasColumnName("color").HasMaxLength(50);
        builder.Property(oi => oi.Size).HasColumnName("size").HasMaxLength(50);
        builder.Property(oi => oi.UnitPrice).HasColumnName("unit_price").HasPrecision(10, 2);
        builder.Property(oi => oi.Quantity).HasColumnName("quantity");
        builder.Property(oi => oi.LineTotal).HasColumnName("line_total").HasPrecision(12, 2);
        builder.Ignore(oi => oi.CreatedAt);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(p => p.OrderId).HasColumnName("order_id");
        builder.Property(p => p.PaymentMethod).HasColumnName("payment_method").HasMaxLength(100);
        builder.Property(p => p.TransactionId).HasColumnName("transaction_id").HasMaxLength(255);
        builder.Property(p => p.Amount).HasColumnName("amount").HasPrecision(12, 2);
        builder.Property(p => p.Status).HasColumnName("status")
            .HasConversion<string>().HasDefaultValue(PaymentStatus.PENDING);
        builder.Property(p => p.FailureReason).HasColumnName("failure_reason").HasMaxLength(500);
        builder.Property(p => p.PaidAt).HasColumnName("paid_at");
        builder.Ignore(p => p.CreatedAt);
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(r => r.UserId).HasColumnName("user_id");
        builder.Property(r => r.ProductId).HasColumnName("product_id");
        builder.HasIndex(r => r.ProductId);
        builder.Property(r => r.Rating).HasColumnName("rating");
        builder.Property(r => r.Comment).HasColumnName("comment").HasColumnType("text");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
    }
}

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(c => c.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.DiscountType).HasColumnName("discount_type").HasConversion<string>();
        builder.Property(c => c.DiscountValue).HasColumnName("discount_value").HasPrecision(10, 2);
        builder.Property(c => c.MinOrderAmount).HasColumnName("min_order_amount").HasPrecision(10, 2);
        builder.Property(c => c.MaxDiscount).HasColumnName("max_discount").HasPrecision(10, 2);
        builder.Property(c => c.ExpiryDate).HasColumnName("expiry_date");
        builder.Property(c => c.UsageLimit).HasColumnName("usage_limit");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
    }
}
