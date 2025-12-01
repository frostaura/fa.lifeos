using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class NetWorthSnapshotConfiguration : IEntityTypeConfiguration<NetWorthSnapshot>
{
    public void Configure(EntityTypeBuilder<NetWorthSnapshot> builder)
    {
        builder.ToTable("net_worth_snapshots");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");
        
        builder.Property(e => e.UserId)
            .IsRequired();
            
        builder.Property(e => e.SnapshotDate)
            .IsRequired();
            
        builder.Property(e => e.TotalAssets)
            .HasPrecision(18, 4);
            
        builder.Property(e => e.TotalLiabilities)
            .HasPrecision(18, 4);
            
        builder.Property(e => e.NetWorth)
            .HasPrecision(18, 4);
            
        builder.Property(e => e.HomeCurrency)
            .HasMaxLength(3)
            .HasDefaultValue("ZAR");
            
        builder.Property(e => e.BreakdownByType)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb");
            
        builder.Property(e => e.BreakdownByCurrency)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb");
        
        builder.Property(e => e.AccountCount)
            .HasDefaultValue(0);
        
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt);
        
        // Unique constraint: one snapshot per user per day
        builder.HasIndex(e => new { e.UserId, e.SnapshotDate })
            .IsUnique();
            
        // Performance index for date-ordered queries
        builder.HasIndex(e => new { e.UserId, e.SnapshotDate })
            .IsDescending(false, true)
            .HasDatabaseName("idx_net_worth_snapshots_user_date_desc");
            
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
