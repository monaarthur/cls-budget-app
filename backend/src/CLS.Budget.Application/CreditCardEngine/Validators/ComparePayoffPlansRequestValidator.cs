using CLS.Budget.Application.CreditCardEngine.Dtos;
using FluentValidation;

namespace CLS.Budget.Application.CreditCardEngine.Validators;

public sealed class ComparePayoffPlansRequestValidator : AbstractValidator<ComparePayoffPlansRequest>
{
    public ComparePayoffPlansRequestValidator()
    {
        RuleFor(x => x.TotalMonthlyDebtPayment).GreaterThan(0);
        RuleFor(x => x.TargetUtilizationPercent)
            .InclusiveBetween(1m, 99m)
            .When(x => x.TargetUtilizationPercent.HasValue);
        RuleFor(x => x.LoanAmount)
            .GreaterThan(0)
            .When(x => x.LoanAmount.HasValue);
        RuleFor(x => x.LoanAnnualPercentageRate)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LoanAmount is > 0);
        RuleFor(x => x.LoanApplyStrategy)
            .Must(s => s is null
                || s.Equals("Avalanche", StringComparison.OrdinalIgnoreCase)
                || s.Equals("Snowball", StringComparison.OrdinalIgnoreCase)
                || s.Equals("SelectedAccounts", StringComparison.OrdinalIgnoreCase))
            .WithMessage("LoanApplyStrategy must be Avalanche, Snowball, SelectedAccounts, or omitted.");
        RuleFor(x => x.LoanApplyCreditCardIds)
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("LoanApplyCreditCardIds must be unique.")
            .Must((req, ids) =>
                !string.Equals(req.LoanApplyStrategy, "SelectedAccounts", StringComparison.OrdinalIgnoreCase)
                || (ids is { Count: > 0 }))
            .When(x => x.LoanAmount is > 0)
            .WithMessage("Select at least one account when applying loan proceeds to specific accounts.");
        RuleFor(x => x.LoanType)
            .Must(t => t is null || LoanScheduleRequestValidator.ParseLoanType(t) is not null)
            .WithMessage("LoanType must be Personal, HomeEquity, Heloc, Retirement401k, Family, or omitted.");
        RuleFor(x => x.LoanTermMonths)
            .GreaterThan(0)
            .When(x => x.LoanTermMonths.HasValue);
        RuleFor(x => x.LoanInterestOnlyMonths)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LoanInterestOnlyMonths.HasValue);
        RuleFor(x => x.LoanFixedMonthlyPayment)
            .GreaterThan(0)
            .When(x => x.LoanFixedMonthlyPayment.HasValue);
    }
}
