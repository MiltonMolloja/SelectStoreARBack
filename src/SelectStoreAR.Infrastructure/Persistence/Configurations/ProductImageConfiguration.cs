using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Infrastructure.Persistence.Configurations;

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Url)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.ThumbnailUrl)
            .HasColumnName("thumbnail_url")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.MediumUrl)
            .HasColumnName("medium_url")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.AltText)
            .HasColumnName("alt_text")
            .HasMaxLength(255);

        builder.Property(i => i.SortOrder)
            .HasColumnName("sort_order");

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(i => i.ProductId).HasDatabaseName("idx_product_images_product");
        builder.HasIndex(i => new { i.ProductId, i.SortOrder }).HasDatabaseName("idx_product_images_order");
    }
}
