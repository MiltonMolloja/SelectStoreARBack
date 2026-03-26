using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.OrderId)
            .HasColumnName("order_id");

        builder.Property(i => i.ProductId)
            .HasColumnName("product_id");

        builder.Property(i => i.ProductName)
            .HasColumnName("product_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(i => i.ProductSlug)
            .HasColumnName("product_slug")
            .HasMaxLength(280)
            .IsRequired();

        builder.Property(i => i.PriceUsd)
            .HasConversion(m => m.Amount, v => Money.FromUsd(v))
            .HasColumnName("price_usd")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(i => i.OrderId).HasDatabaseName("idx_order_items_order");
    }
}
