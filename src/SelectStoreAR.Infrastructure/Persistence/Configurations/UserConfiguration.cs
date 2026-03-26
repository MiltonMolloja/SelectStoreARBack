using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("idx_users_email");

        builder.Property(u => u.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(u => u.Role).HasDatabaseName("idx_users_role");

        builder.Property(u => u.PictureUrl)
            .HasColumnName("picture_url")
            .HasMaxLength(500);

        builder.Property(u => u.Phone)
            .HasMaxLength(20);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at")
            .HasColumnType("timestamptz");

        builder.HasMany(u => u.ExternalLogins)
            .WithOne()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
