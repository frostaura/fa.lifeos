using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class UserXPConfiguration : IEntityTypeConfiguration<UserXP>
{
    public void Configure(EntityTypeBuilder<UserXP> builder)
    {
        builder.ToTable("user_xps");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(e => e.UserId)
            .IsRequired();
            
        builder.Property(e => e.TotalXp)
            .HasDefaultValue(0L);
            
        builder.Property(e => e.Level)
            .HasDefaultValue(1);
            
        builder.Property(e => e.WeeklyXp)
            .HasDefaultValue(0);
            
        builder.Property(e => e.WeekStartDate)
            .IsRequired();
        
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);
        
        // Unique constraint: one XP record per user
        builder.HasIndex(e => e.UserId)
            .IsUnique();
        
        // Relationships
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
