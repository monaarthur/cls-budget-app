using CLS.Budget.Application.Incomes.Dtos;
using FluentValidation;

namespace CLS.Budget.Application.Incomes.Validators;

public sealed class UpdateIncomeRequestValidator : AbstractValidator<UpdateIncomeRequest>
{
    public UpdateIncomeRequestValidator()
    {
        RuleFor(x => x.BudgetId).GreaterThan(0);
        RuleFor(x => x.IncomeSourceId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReceivedDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
