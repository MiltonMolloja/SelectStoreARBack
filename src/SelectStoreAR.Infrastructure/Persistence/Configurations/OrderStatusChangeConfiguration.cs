using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Infrastructure.Persistence.Configurations;

public sealed class OrderStatusChangeConfiguration : IEntityTypeConfiguration<OrderStatusChange>
{
    public void Configure(EntityTypeBuilder<OrderStatusChange> builder)
    {
        builder.ToTable("order_status_changes");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.OrderId)
            .HasColumnName("order_id");

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(s => s.Notes)
            .HasColumnType("text");

        builder.Property(s => s.ChangedAt)
            .HasColumnName("changed_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(s => s.OrderId).HasDatabaseName("idx_order_status_changes_order");
        builder.HasIndex(s => s.ChangedAt).HasDatabaseName("idx_order_status_changes_date");
    }
}
