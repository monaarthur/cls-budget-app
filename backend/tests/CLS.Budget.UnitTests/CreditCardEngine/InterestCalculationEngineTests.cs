using CLS.Budget.Domain.CreditCardEngine.Interest;
using FluentAssertions;

namespace CLS.Budget.UnitTests.CreditCardEngine;

public sealed class InterestCalculationEngineTests
{
    private readonly InterestCalculationEngine _sut = new();

    [Fact]
    public void Calculate_GoldenCase_FirstMonthInterestApproximatelyTen()
    {
        var result = _sut.Calculate(new InterestCalculationRequest(
            Balance: 1000m,
            AnnualPercentageRate: 12m,
            MonthlyPayment: 100m,
            StartDate: new DateOnly(2026, 1, 1)));

        result.EstimatedMonthlyInterest.Should().Be(10m);
        result.NegativeAmortizationDetected.Should().BeFalse();
        result.EstimatedPayoffDate.Should().NotBeNull();
        result.NumberOfPayments.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Calculate_ZeroBalance_ReturnsZeroInterest()
    {
        var result = _sut.Calculate(new InterestCalculationRequest(
            Balance: 0m,
            AnnualPercentageRate: 22m,
            MonthlyPayment: 50m,
            StartDate: new DateOnly(2026, 1, 1)));

        result.TotalInterestPaid.Should().Be(0m);
        result.NumberOfPayments.Should().Be(0);
    }

    [Fact]
    public void Calculate_PaymentBelowInterest_FlagsNegativeAmortization()
    {
        var result = _sut.Calculate(new InterestCalculationRequest(
            Balance: 10000m,
            AnnualPercentageRate: 24m,
            MonthlyPayment: 10m,
            StartDate: new DateOnly(2026, 1, 1)));

        result.NegativeAmortizationDetected.Should().BeTrue();
        result.EstimatedPayoffDate.Should().BeNull();
    }

    [Fact]
    public void Calculate_UsesPromotionalAprBeforeExpiration()
    {
        var result = _sut.Calculate(new InterestCalculationRequest(
            Balance: 1000m,
            AnnualPercentageRate: 24m,
            MonthlyPayment: 100m,
            StartDate: new DateOnly(2026, 1, 1),
            PromotionalAnnualPercentageRate: 0m,
            PromotionalRateExpirationDate: new DateOnly(2026, 6, 1)));

        result.EstimatedMonthlyInterest.Should().Be(0m);
    }
}
