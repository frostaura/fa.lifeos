using FluentValidation;
using LifeOS.Application.Commands.Tasks;

namespace LifeOS.Application.Validators;

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters");

        RuleFor(x => x.TaskType)
            .Must(t => new[] { "habit", "one_off", "scheduled_event" }.Contains(t.ToLowerInvariant()))
            .WithMessage("TaskType must be one of: habit, one_off, scheduled_event");

        RuleFor(x => x.Frequency)
            .Must(f => new[] { "daily", "weekly", "monthly", "quarterly", "yearly", "ad_hoc" }.Contains(f.ToLowerInvariant()))
            .WithMessage("Frequency must be one of: daily, weekly, monthly, quarterly, yearly, ad_hoc");

        RuleFor(x => x.ScheduledDate)
            .NotNull().When(x => x.TaskType.ToLowerInvariant() == "scheduled_event")
            .WithMessage("ScheduledDate is required for scheduled_event task type");
    }
}

public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Frequency)
            .Must(f => string.IsNullOrEmpty(f) || new[] { "daily", "weekly", "monthly", "quarterly", "yearly", "ad_hoc" }.Contains(f.ToLowerInvariant()))
            .WithMessage("Frequency must be one of: daily, weekly, monthly, quarterly, yearly, ad_hoc");
    }
}
