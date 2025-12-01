using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement>
{
    public void Configure(EntityTypeBuilder<UserAchievement> builder)
    {
        builder.ToTable("user_achievements");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(e => e.UserId)
            .IsRequired();
            
        builder.Property(e => e.AchievementId)
            .IsRequired();
            
        builder.Property(e => e.UnlockedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        builder.Property(e => e.Progress)
            .HasDefaultValue(100);
            
        builder.Property(e => e.UnlockContext)
            .HasMaxLength(500);
        
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);
        
        // Unique constraint: one achievement per user
        builder.HasIndex(e => new { e.UserId, e.AchievementId })
            .IsUnique();
            
        // Performance indexes
        builder.HasIndex(e => e.UserId);
        
        // Relationships
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(e => e.Achievement)
            .WithMany(a => a.UserAchievements)
            .HasForeignKey(e => e.AchievementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
