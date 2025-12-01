using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.KeyPrefix)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.KeyHash)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Scopes)
            .HasMaxLength(500)
            .HasDefaultValue("metrics:write");

        builder.HasIndex(e => e.KeyPrefix);
        builder.HasIndex(e => new { e.UserId, e.IsRevoked });

        builder.HasOne(e => e.User)
            .WithMany(u => u.ApiKeys)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
