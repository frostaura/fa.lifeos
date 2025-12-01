using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class ApiEventLogConfiguration : IEntityTypeConfiguration<ApiEventLog>
{
    public void Configure(EntityTypeBuilder<ApiEventLog> builder)
    {
        builder.ToTable("api_event_logs");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.EventType)
            .HasMaxLength(50)
            .IsRequired();
            
        builder.Property(e => e.Source)
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(e => e.ApiKeyPrefix)
            .HasMaxLength(50);
            
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .IsRequired();
            
        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);
            
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => e.EventType);
        
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
