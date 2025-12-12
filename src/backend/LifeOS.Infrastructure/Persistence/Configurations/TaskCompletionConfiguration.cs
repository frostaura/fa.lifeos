using LifeOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeOS.Infrastructure.Persistence.Configurations;

public class TaskCompletionConfiguration : IEntityTypeConfiguration<TaskCompletion>
{
    public void Configure(EntityTypeBuilder<TaskCompletion> builder)
    {
        builder.ToTable("task_completions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.TaskId)
            .HasColumnName("task_id")
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.CompletionType)
            .HasColumnName("completion_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(Domain.Enums.CompletionType.Manual)
            .IsRequired();

        builder.Property(e => e.ValueNumber)
            .HasColumnName("value_number")
            .HasPrecision(18, 4);

        builder.Property(e => e.Notes)
            .HasColumnName("notes");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(e => e.Task)
            .WithMany(t => t.TaskCompletions)
            .HasForeignKey(e => e.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany(u => u.TaskCompletions)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes per database.md specification
        builder.HasIndex(e => e.TaskId)
            .HasDatabaseName("idx_task_completions_task_id");
            
        builder.HasIndex(e => new { e.UserId, e.CompletedAt })
            .HasDatabaseName("idx_task_completions_user_id_completed_at");
    }
}
