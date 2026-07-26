using CLS.Budget.Domain.CreditCardEngine.Forecast;
using CLS.Budget.Domain.CreditCardEngine.Payoff;
using FluentAssertions;

namespace CLS.Budget.UnitTests.CreditCardEngine;

public sealed class ForecastEngineTests
{
    private readonly PayoffStrategyEngine _payoff = new();
    private readonly ForecastEngine _sut;

    public ForecastEngineTests()
    {
        _sut = new ForecastEngine(_payoff);
    }

    [Fact]
    public void Generate_MatchesPayoffEngineFirstMonths()
    {
        var cards = TwoCards();
        var start = new DateOnly(2026, 1, 1);
        var payment = 400m;

        var payoff = _payoff.GeneratePlan(new PayoffPlanRequest(
            cards, payment, PayoffStrategyType.Avalanche, start));
        var forecast = _sut.Generate(new ForecastRequest(
            cards, PayoffStrategyType.Avalanche, payment, start, ForecastMonths: 6));

        forecast.IsValid.Should().BeTrue();
        payoff.IsValid.Should().BeTrue();

        foreach (var month in forecast.Months)
        {
            var payoffMonth = payoff.Schedule.Where(s => s.Month == month.Month).ToList();
            if (payoffMonth.Count == 0)
            {
                month.EndingDebt.Should().Be(0m);
                continue;
            }

            month.StartingDebt.Should().Be(payoffMonth.Sum(s => s.StartingBalance));
            month.Interest.Should().Be(payoffMonth.Sum(s => s.InterestCharged));
            month.Payments.Should().Be(payoffMonth.Sum(s => s.PaymentApplied));
            month.EndingDebt.Should().Be(payoffMonth.Sum(s => s.EndingBalance));
        }
    }

    [Fact]
    public void Generate_SupportsAtLeast120Months()
    {
        var result = _sut.Generate(new ForecastRequest(
            TwoCards(),
            PayoffStrategyType.Snowball,
            TotalMonthlyDebtPayment: 400m,
            StartDate: new DateOnly(2026, 1, 1),
            ForecastMonths: 120));

        result.IsValid.Should().BeTrue();
        result.Months.Count.Should().Be(120);
    }

    [Fact]
    public void Generate_AppliesOneTimePaymentAndWarnsWhenDebtIncreases()
    {
        var result = _sut.Generate(new ForecastRequest(
            TwoCards(),
            PayoffStrategyType.Avalanche,
            TotalMonthlyDebtPayment: 150m,
            StartDate: new DateOnly(2026, 1, 1),
            ForecastMonths: 3,
            AdditionalCharges:
            [
                new ForecastCharge(MonthOffset: 1, Amount: 2000m)
            ],
            OneTimePayments:
            [
                new ForecastOneTimePayment(MonthOffset: 0, Amount: 50m)
            ]));

        result.IsValid.Should().BeTrue();
        result.Months[1].NewCharges.Should().Be(2000m);
        result.Warnings.Should().Contain(w =>
            w.Contains("increased", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Generate_TracksAvailableCash()
    {
        var result = _sut.Generate(new ForecastRequest(
            TwoCards(),
            PayoffStrategyType.Avalanche,
            TotalMonthlyDebtPayment: 400m,
            StartDate: new DateOnly(2026, 1, 1),
            ForecastMonths: 2,
            MonthlyNetIncome: 5000m,
            MonthlyExpenses: 3000m));

        var first = result.Months[0];
        first.AvailableCash.Should().Be(5000m - 3000m - first.Payments);
    }

    private static List<CreditCardPayoffInput> TwoCards() =>
    [
        new(
            CreditCardId: 1,
            Name: "High APR",
            CurrentBalance: 3000m,
            CreditLimit: 5000m,
            AnnualPercentageRate: 24m,
            FixedMonthlyPayment: 100m,
            MinimumPaymentPercentage: null,
            MinimumPaymentFloor: null,
            PromotionalAnnualPercentageRate: null,
            PromotionalRateExpirationDate: null),
        new(
            CreditCardId: 2,
            Name: "Small Balance",
            CurrentBalance: 800m,
            CreditLimit: 2000m,
            AnnualPercentageRate: 12m,
            FixedMonthlyPayment: 50m,
            MinimumPaymentPercentage: null,
            MinimumPaymentFloor: null,
            PromotionalAnnualPercentageRate: null,
            PromotionalRateExpirationDate: null)
    ];
}
