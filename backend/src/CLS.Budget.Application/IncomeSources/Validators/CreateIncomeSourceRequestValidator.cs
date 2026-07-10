using FluentValidation;
using CLS.Budget.Application.IncomeSources.Dtos;

namespace CLS.Budget.Application.IncomeSources.Validators;

public sealed class CreateIncomeSourceRequestValidator : AbstractValidator<CreateIncomeSourceRequest>
{
    public CreateIncomeSourceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
