using CLS.Budget.Application.CreditCardEngine.Dtos;
using FluentValidation;

namespace CLS.Budget.Application.CreditCardEngine.Validators;

public sealed class AnalyzeBalanceTransferRequestValidator : AbstractValidator<AnalyzeBalanceTransferRequest>
{
    public AnalyzeBalanceTransferRequestValidator()
    {
        RuleFor(x => x.TransferAmount).GreaterThan(0);
        RuleFor(x => x.PromotionalPeriodMonths).InclusiveBetween(1, 120);
        RuleFor(x => x.CurrentAnnualPercentageRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PromotionalAnnualPercentageRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NewRegularAnnualPercentageRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TransferFeePercentage).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TransferFeeFlatAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PlannedMonthlyPayment).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AvailableTransferLimit).GreaterThanOrEqualTo(0);
    }
}
