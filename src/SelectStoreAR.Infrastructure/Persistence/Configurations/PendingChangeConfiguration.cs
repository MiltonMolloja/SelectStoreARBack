using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración EF Core para la tabla product_pending_changes.
/// </summary>
public sealed class PendingChangeConfiguration : IEntityTypeConfiguration<ProductPendingChange>
{
    public void Configure(EntityTypeBuilder<ProductPendingChange> builder)
    {
        builder.ToTable("product_pending_changes");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TelegramSyncBatchId)
            .IsRequired();

        builder.Property(p => p.TelegramMessageId)
            .HasColumnName("telegram_msg_id")
            .HasMaxLength(50);

        builder.Property(p => p.ChangeType)
            .HasConversion<string>()
            .HasColumnName("change_type")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.RawTelegramText)
            .HasColumnName("raw_telegram_text")
            .HasColumnType("text");

        builder.Property(p => p.ProposedName)
            .HasColumnName("proposed_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.ProposedBrand)
            .HasColumnName("proposed_brand")
            .HasMaxLength(100);

        builder.Property(p => p.ProposedDescription)
            .HasColumnName("proposed_description")
            .HasColumnType("text");

        builder.Property(p => p.ProposedPriceUsd)
            .HasConversion(m => m.Amount, v => Money.FromUsd(v))
            .HasColumnName("proposed_price_usd")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.ProposedAvailability)
            .HasConversion<string>()
            .HasColumnName("proposed_availability")
            .HasMaxLength(20);

        builder.Property(p => p.ProposedInspiration)
            .HasColumnName("proposed_inspiration")
            .HasMaxLength(200);

        builder.Property(p => p.ProposedCategory)
            .HasColumnName("proposed_category")
            .HasMaxLength(50);

        builder.Property(p => p.CurrentPriceUsd)
            .HasConversion(
                m => m != null ? (decimal?)m.Amount : null,
                v => v.HasValue ? Money.FromUsd(v.Value) : null)
            .HasColumnName("current_price_usd")
            .HasPrecision(10, 2);

        builder.Property(p => p.ReviewedAt)
            .HasColumnName("reviewed_at")
            .HasColumnType("timestamptz");

        builder.Property(p => p.ReviewedBy)
            .HasColumnName("reviewed_by")
            .HasMaxLength(100);

        builder.Property(p => p.ReviewNote)
            .HasColumnName("review_note")
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        // Relationships
        builder.HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(p => p.Status).HasDatabaseName("idx_pending_status");
        builder.HasIndex(p => p.ProductId).HasDatabaseName("idx_pending_product");
        builder.HasIndex(p => p.TelegramSyncBatchId).HasDatabaseName("idx_pending_batch");
    }
}
