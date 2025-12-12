using FluentValidation;
using LifeOS.Application.DTOs.Metrics;

namespace LifeOS.Application.Validators;

public class MetricRecordRequestValidator : AbstractValidator<MetricRecordRequest>
{
    public MetricRecordRequestValidator()
    {
        RuleFor(x => x.Timestamp)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Timestamp cannot be in the future");

        RuleFor(x => x.Source)
            .NotEmpty()
            .WithMessage("Source is required")
            .MaximumLength(100)
            .WithMessage("Source must not exceed 100 characters");

        RuleFor(x => x.Metrics)
            .NotNull()
            .WithMessage("Metrics object is required")
            .Must(m => m != null && m.Count > 0)
            .WithMessage("At least one metric must be provided");
    }
}
