using FluentValidation;
using LifeOS.Application.Commands.Dimensions;

namespace LifeOS.Application.Validators;

public class UpdateDimensionWeightCommandValidator : AbstractValidator<UpdateDimensionWeightCommand>
{
    public UpdateDimensionWeightCommandValidator()
    {
        RuleFor(x => x.Weight)
            .InclusiveBetween(0m, 1m).WithMessage("Weight must be between 0 and 1");
    }
}
