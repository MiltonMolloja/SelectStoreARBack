using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Infrastructure.Persistence.Configurations;

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("exchange_rates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Rate)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(e => e.Type)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => e.IsActive).HasDatabaseName("idx_exchange_rates_active");
    }
}
