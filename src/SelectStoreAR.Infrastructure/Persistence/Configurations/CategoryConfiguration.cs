using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SelectStoreAR.Domain.Entities;
using SelectStoreAR.Domain.ValueObjects;

namespace SelectStoreAR.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Slug)
            .HasConversion(s => s.Value, v => Slug.Create(v))
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(c => c.Slug).IsUnique();

        builder.Property(c => c.DefaultMarkup)
            .HasConversion(
                m => m != null ? (decimal?)m.Percentage : null,
                v => v.HasValue ? Markup.Create(v.Value) : null)
            .HasPrecision(5, 2)
            .HasColumnName("default_markup");

        builder.Property(c => c.ImageUrl)
            .HasMaxLength(500)
            .HasColumnName("image_url");

        builder.Property(c => c.SortOrder)
            .HasColumnName("sort_order");

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.ParentId).HasDatabaseName("idx_categories_parent");
        builder.HasIndex(c => c.SortOrder).HasDatabaseName("idx_categories_sort");
    }
}
