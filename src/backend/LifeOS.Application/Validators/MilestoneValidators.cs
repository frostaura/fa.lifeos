using FluentValidation;
using LifeOS.Application.Commands.Milestones;

namespace LifeOS.Application.Validators;

public class CreateMilestoneCommandValidator : AbstractValidator<CreateMilestoneCommand>
{
    public CreateMilestoneCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters");

        RuleFor(x => x.DimensionId)
            .NotEmpty().WithMessage("DimensionId is required");

        RuleFor(x => x.TargetMetricValue)
            .NotNull().When(x => !string.IsNullOrEmpty(x.TargetMetricCode))
            .WithMessage("TargetMetricValue is required when TargetMetricCode is provided");
    }
}

public class UpdateMilestoneCommandValidator : AbstractValidator<UpdateMilestoneCommand>
{
    public UpdateMilestoneCommandValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrEmpty(s) || new[] { "active", "completed", "abandoned" }.Contains(s.ToLowerInvariant()))
            .WithMessage("Status must be one of: active, completed, abandoned");
    }
}
