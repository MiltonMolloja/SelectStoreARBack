using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Infrastructure.Persistence.Configurations;

public sealed class SiteConfigConfiguration : IEntityTypeConfiguration<SiteConfig>
{
    public void Configure(EntityTypeBuilder<SiteConfig> builder)
    {
        builder.ToTable("site_configs");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(s => s.Key).IsUnique().HasDatabaseName("idx_site_configs_key");

        builder.Property(s => s.Value)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");
    }
}
