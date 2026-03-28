using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración EF Core para la tabla price_history.
/// </summary>
public sealed class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.ToTable("price_history");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductId)
            .IsRequired();

        builder.Property(p => p.PriceUsd)
            .HasConversion(m => m.Amount, v => Money.FromUsd(v))
            .HasColumnName("price_usd")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.Source)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.ChangedAt)
            .HasColumnName("changed_at")
            .HasColumnType("timestamptz");

        builder.Property(p => p.ChangedBy)
            .HasColumnName("changed_by")
            .HasMaxLength(100);

        // Relationships
        builder.HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(p => p.ProductId).HasDatabaseName("idx_price_history_product");
    }
}
