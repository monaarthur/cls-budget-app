using CLS.Budget.Application.AutoPayoff.Dtos;
using FluentValidation;

namespace CLS.Budget.Application.AutoPayoff.Validators;

public sealed class SaveAutoPayoffConfigRequestValidator : AbstractValidator<SaveAutoPayoffConfigRequest>
{
    public SaveAutoPayoffConfigRequestValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Auto Budget name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Auto Budget name is required.")
            .MaximumLength(200)
            .WithMessage("Auto Budget name must be 200 characters or fewer.");

        RuleFor(x => x.ExtraMonthlyAmount)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(1_000_000);

        RuleForEach(x => x.ExcludeStatusIds).GreaterThan(0);
        RuleForEach(x => x.ExcludeCategoryIds).GreaterThan(0);
        RuleForEach(x => x.ExcludeAccountIds).GreaterThan(0);
        RuleForEach(x => x.TargetCategoryIds).GreaterThan(0);
    }
}
