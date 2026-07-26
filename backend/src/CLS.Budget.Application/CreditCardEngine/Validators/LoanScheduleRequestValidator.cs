using CLS.Budget.Application.CreditCardEngine.Dtos;
using CLS.Budget.Domain.CreditCardEngine.Loan;
using FluentValidation;

namespace CLS.Budget.Application.CreditCardEngine.Validators;

public sealed class LoanScheduleRequestValidator : AbstractValidator<LoanScheduleRequestDto>
{
    public LoanScheduleRequestValidator()
    {
        RuleFor(x => x.LoanType)
            .Must(t => ParseLoanType(t) is not null)
            .WithMessage("LoanType must be Personal, HomeEquity, Heloc, Retirement401k, or Family.");
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.AnnualPercentageRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TermMonths)
            .GreaterThan(0)
            .When(x => ParseLoanType(x.LoanType) is LoanType.Personal
                or LoanType.HomeEquity
                or LoanType.Retirement401k
                or LoanType.Heloc);
        RuleFor(x => x.InterestOnlyMonths)
            .GreaterThanOrEqualTo(0)
            .When(x => x.InterestOnlyMonths.HasValue);
        RuleFor(x => x.FixedMonthlyPayment)
            .GreaterThan(0)
            .When(x => ParseLoanType(x.LoanType) == LoanType.Family);
    }

    public static LoanType? ParseLoanType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "personal" => LoanType.Personal,
            "homeequity" or "home_equity" or "secondmortgage" => LoanType.HomeEquity,
            "heloc" => LoanType.Heloc,
            "retirement401k" or "401k" or "401(k)" => LoanType.Retirement401k,
            "family" or "private" => LoanType.Family,
            _ => null
        };
    }

    public static string? ToLoanTypeLabel(LoanType? type) => type switch
    {
        LoanType.Personal => "Personal",
        LoanType.HomeEquity => "HomeEquity",
        LoanType.Heloc => "Heloc",
        LoanType.Retirement401k => "Retirement401k",
        LoanType.Family => "Family",
        _ => null
    };
}
