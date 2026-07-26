using CLS.Budget.Application.CreditCardEngine.Dtos;
using FluentValidation;

namespace CLS.Budget.Application.CreditCardEngine.Validators;

public sealed class AnalyzeCashFlowRequestValidator : AbstractValidator<AnalyzeCashFlowRequest>
{
    public AnalyzeCashFlowRequestValidator()
    {
        RuleFor(x => x.MonthlyNetIncome).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RequiredExpenses).GreaterThanOrEqualTo(0);
        RuleFor(x => x.VariableExpenses).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ExistingDebtMinimums)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ExistingDebtMinimums.HasValue);
        RuleFor(x => x.EmergencySavingsContribution).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SafetyBuffer).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AdditionalAvailableFunds).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UserOverrideExtraPayment)
            .GreaterThanOrEqualTo(0)
            .When(x => x.UserOverrideExtraPayment.HasValue);
    }
}
