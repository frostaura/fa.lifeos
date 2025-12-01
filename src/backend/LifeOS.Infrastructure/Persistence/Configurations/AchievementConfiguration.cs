using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.ToTable("achievements");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(e => e.Code)
            .HasMaxLength(50)
            .IsRequired();
            
        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(e => e.Description)
            .HasMaxLength(500);
            
        builder.Property(e => e.Icon)
            .HasMaxLength(50);
            
        builder.Property(e => e.XpValue)
            .HasDefaultValue(0);
            
        builder.Property(e => e.Category)
            .HasMaxLength(50)
            .IsRequired();
            
        builder.Property(e => e.Tier)
            .HasMaxLength(20)
            .HasDefaultValue("bronze");
            
        builder.Property(e => e.UnlockCondition)
            .IsRequired();
            
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);
            
        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);
        
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);
        
        // Unique constraint on code
        builder.HasIndex(e => e.Code)
            .IsUnique();
            
        // Performance indexes
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.IsActive);
    }
}
