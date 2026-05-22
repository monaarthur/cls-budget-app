using CLS.Budget.Application.Budgets.Dtos;
using FluentValidation;

namespace CLS.Budget.Application.Budgets.Validators;

public sealed class UpdateBudgetRequestValidator : AbstractValidator<UpdateBudgetRequest>
{
    public UpdateBudgetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BudgetTemplateId).GreaterThan(0);
        RuleFor(x => x.EndPeriod).GreaterThanOrEqualTo(x => x.StartPeriod);
    }
}
