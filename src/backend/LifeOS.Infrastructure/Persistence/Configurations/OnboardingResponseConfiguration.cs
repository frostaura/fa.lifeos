using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class OnboardingResponseConfiguration : IEntityTypeConfiguration<OnboardingResponse>
{
    public void Configure(EntityTypeBuilder<OnboardingResponse> builder)
    {
        builder.ToTable("onboarding_responses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.StepCode)
            .HasColumnName("step_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ResponseData)
            .HasColumnName("response_data")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(e => e.User)
            .WithMany(u => u.OnboardingResponses)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
