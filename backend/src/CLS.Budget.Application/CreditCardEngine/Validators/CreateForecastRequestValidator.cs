using CLS.Budget.Application.CreditCardEngine.Dtos;
using CLS.Budget.Domain.CreditCardEngine.Forecast;
using CLS.Budget.Domain.CreditCardEngine.Payoff;
using FluentValidation;

namespace CLS.Budget.Application.CreditCardEngine.Validators;

public sealed class CreateForecastRequestValidator : AbstractValidator<CreateForecastRequest>
{
    public CreateForecastRequestValidator()
    {
        RuleFor(x => x.TotalMonthlyDebtPayment).GreaterThan(0);
        RuleFor(x => x.ForecastMonths)
            .InclusiveBetween(ForecastEngine.MinForecastMonths, ForecastEngine.MaxForecastMonths);
        RuleFor(x => x.Strategy)
            .Must(s => ParseStrategy(s) is not null)
            .WithMessage("Strategy must be Avalanche, Snowball, or MinimumsOnly.");
        RuleFor(x => x.MonthlyNetIncome)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MonthlyNetIncome.HasValue);
        RuleFor(x => x.MonthlyExpenses)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MonthlyExpenses.HasValue);
        RuleFor(x => x.TargetUtilizationPercent)
            .InclusiveBetween(1, 99)
            .When(x => x.TargetUtilizationPercent.HasValue);
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.Save);
    }

    internal static PayoffStrategyType? ParseStrategy(string? strategy) =>
        strategy?.Trim().ToLowerInvariant() switch
        {
            "avalanche" => PayoffStrategyType.Avalanche,
            "snowball" => PayoffStrategyType.Snowball,
            "minimumsonly" or "minimums" or "minimums-only" => PayoffStrategyType.MinimumsOnly,
            _ => null
        };
}
