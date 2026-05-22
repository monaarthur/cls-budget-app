using CLS.Budget.Application.Budgets.Dtos;
using FluentValidation;

namespace CLS.Budget.Application.Budgets.Validators;

public sealed class CopyBudgetRequestValidator : AbstractValidator<CopyBudgetRequest>
{
    public CopyBudgetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BudgetTemplateId).GreaterThan(0).When(x => x.BudgetTemplateId.HasValue);
        RuleFor(x => x.EndPeriod).GreaterThanOrEqualTo(x => x.StartPeriod);
    }
}
