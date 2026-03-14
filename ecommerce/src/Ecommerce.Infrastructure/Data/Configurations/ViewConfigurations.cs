using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Data.Configurations;

/// <summary>
/// Maps v_inventory_available SQL view as a keyless read-only entity.
/// </summary>
public class InventoryAvailableViewConfiguration : IEntityTypeConfiguration<InventoryAvailableView>
{
    public void Configure(EntityTypeBuilder<InventoryAvailableView> builder)
    {
        builder.ToView("v_inventory_available");
        builder.HasNoKey();

        builder.Property(v => v.Id).HasColumnName("id");
        builder.Property(v => v.VariantId).HasColumnName("variant_id");
        builder.Property(v => v.Sku).HasColumnName("sku");
        builder.Property(v => v.ProductId).HasColumnName("product_id");
        builder.Property(v => v.StockQuantity).HasColumnName("stock_quantity");
        builder.Property(v => v.ReservedQuantity).HasColumnName("reserved_quantity");
        builder.Property(v => v.AvailableQuantity).HasColumnName("available_quantity");
        builder.Property(v => v.UpdatedAt).HasColumnName("updated_at");
    }
}

/// <summary>
/// Maps v_product_catalogue SQL view as a keyless read-only entity.
/// </summary>
public class ProductCatalogueViewConfiguration : IEntityTypeConfiguration<ProductCatalogueView>
{
    public void Configure(EntityTypeBuilder<ProductCatalogueView> builder)
    {
        builder.ToView("v_product_catalogue");
        builder.HasNoKey();

        builder.Property(v => v.Id).HasColumnName("id");
        builder.Property(v => v.Name).HasColumnName("name");
        builder.Property(v => v.Slug).HasColumnName("slug");
        builder.Property(v => v.Brand).HasColumnName("brand");
        builder.Property(v => v.BasePrice).HasColumnName("base_price").HasPrecision(10, 2);
        builder.Property(v => v.IsActive).HasColumnName("is_active");
        builder.Property(v => v.CategoryId).HasColumnName("category_id");
        builder.Property(v => v.CategoryName).HasColumnName("category_name");
        builder.Property(v => v.CategorySlug).HasColumnName("category_slug");
        builder.Property(v => v.PrimaryImageUrl).HasColumnName("primary_image_url");
        builder.Property(v => v.CreatedAt).HasColumnName("created_at");
    }
}

/// <summary>
/// Maps v_order_summary SQL view as a keyless read-only entity.
/// </summary>
public class OrderSummaryViewConfiguration : IEntityTypeConfiguration<OrderSummaryView>
{
    public void Configure(EntityTypeBuilder<OrderSummaryView> builder)
    {
        builder.ToView("v_order_summary");
        builder.HasNoKey();

        builder.Property(v => v.OrderId).HasColumnName("order_id");
        builder.Property(v => v.UserId).HasColumnName("user_id");
        builder.Property(v => v.UserName).HasColumnName("user_name");
        builder.Property(v => v.UserEmail).HasColumnName("user_email");
        builder.Property(v => v.OrderStatus).HasColumnName("order_status");
        builder.Property(v => v.SubtotalAmount).HasColumnName("subtotal_amount").HasPrecision(12, 2);
        builder.Property(v => v.DiscountAmount).HasColumnName("discount_amount").HasPrecision(12, 2);
        builder.Property(v => v.TotalAmount).HasColumnName("total_amount").HasPrecision(12, 2);
        builder.Property(v => v.CreatedAt).HasColumnName("created_at");
        builder.Property(v => v.CouponCode).HasColumnName("coupon_code");
        builder.Property(v => v.PaymentStatus).HasColumnName("payment_status");
    }
}
