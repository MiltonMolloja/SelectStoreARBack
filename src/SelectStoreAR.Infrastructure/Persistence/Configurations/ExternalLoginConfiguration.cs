using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Infrastructure.Persistence.Configurations;

public sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("external_logins");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId)
            .HasColumnName("user_id");

        builder.Property(e => e.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ProviderKey)
            .HasColumnName("provider_key")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.Provider, e.ProviderKey })
            .IsUnique()
            .HasDatabaseName("idx_external_logins_provider");

        builder.HasIndex(e => e.UserId).HasDatabaseName("idx_external_logins_user");
    }
}
