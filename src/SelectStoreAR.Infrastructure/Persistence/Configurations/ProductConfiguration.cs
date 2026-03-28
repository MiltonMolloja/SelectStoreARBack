using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasConversion(s => s.Value, v => Slug.Create(v))
            .HasMaxLength(280)
            .IsRequired();

        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.Description)
            .HasColumnType("text");

        builder.Property(p => p.Brand)
            .HasMaxLength(100);

        builder.Property(p => p.BasePriceUsd)
            .HasConversion(m => m.Amount, v => Money.FromUsd(v))
            .HasColumnName("base_price_usd")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.MarkupPercentage)
            .HasConversion(
                m => m != null ? (decimal?)m.Percentage : null,
                v => v.HasValue ? Markup.Create(v.Value) : null)
            .HasColumnName("markup_percentage")
            .HasPrecision(5, 2);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.IsFeatured)
            .HasColumnName("is_featured");

        builder.Property(p => p.Specifications)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb");

        builder.Property(p => p.TelegramMessageId)
            .HasColumnName("telegram_msg_id")
            .HasMaxLength(50);

        builder.Property(p => p.Availability)
            .HasConversion<string>()
            .HasColumnName("availability")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Inspiration)
            .HasColumnName("inspiration")
            .HasMaxLength(200);

        builder.Property(p => p.LastTelegramRaw)
            .HasColumnName("last_telegram_raw")
            .HasColumnType("text");

        builder.Property(p => p.LastSyncedAt)
            .HasColumnName("last_synced_at")
            .HasColumnType("timestamptz");

        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted");

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        // Full-text search vector — maintained by PostgreSQL trigger (see migration)
        // The column is defined in SQL but not mapped as a generated column in EF

        // Soft delete global filter
        builder.HasQueryFilter(p => !p.IsDeleted);

        // Relationships
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(p => p.CategoryId).HasDatabaseName("idx_products_category");
        builder.HasIndex(p => p.Status).HasDatabaseName("idx_products_status");
        builder.HasIndex(p => p.CreatedAt).HasDatabaseName("idx_products_created");
        builder.HasIndex(p => p.IsFeatured).HasDatabaseName("idx_products_featured");
        builder.HasIndex(p => p.TelegramMessageId).HasDatabaseName("idx_products_telegram");
    }
}
