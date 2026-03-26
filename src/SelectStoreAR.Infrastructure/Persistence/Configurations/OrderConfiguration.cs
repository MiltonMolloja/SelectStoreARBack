using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .HasColumnName("order_number")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(o => o.OrderNumber).IsUnique().HasDatabaseName("idx_orders_number");

        builder.Property(o => o.UserId)
            .HasColumnName("user_id");

        builder.Property(o => o.CustomerName)
            .HasColumnName("customer_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(o => o.CustomerPhone)
            .HasConversion(p => p.Value, v => PhoneNumber.Create(v))
            .HasColumnName("customer_phone")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.TotalUsd)
            .HasConversion(m => m.Amount, v => Money.FromUsd(v))
            .HasColumnName("total_usd")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(o => o.TotalArs)
            .HasConversion(m => m.Amount, v => Money.FromArs(v))
            .HasColumnName("total_ars")
            .HasPrecision(15, 0)
            .IsRequired();

        builder.Property(o => o.ExchangeRateUsed)
            .HasColumnName("exchange_rate")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.DepositType)
            .HasColumnName("deposit_type")
            .HasMaxLength(20);

        builder.Property(o => o.Notes)
            .HasColumnType("text");

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.StatusHistory)
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.Status).HasDatabaseName("idx_orders_status");
        builder.HasIndex(o => o.CreatedAt).HasDatabaseName("idx_orders_created");
        builder.HasIndex(o => o.UserId).HasDatabaseName("idx_orders_user");
    }
}
