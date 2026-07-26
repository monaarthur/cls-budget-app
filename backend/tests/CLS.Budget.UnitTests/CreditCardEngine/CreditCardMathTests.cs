using CLS.Budget.Domain.CreditCardEngine;
using FluentAssertions;

namespace CLS.Budget.UnitTests.CreditCardEngine;

public sealed class CreditCardMathTests
{
    [Fact]
    public void CalculateMinimumPayment_UsesPercentAndFloor()
    {
        var min = CreditCardMath.CalculateMinimumPayment(
            balance: 1000m,
            percentagePoints: 2m,
            floor: 25m);

        min.Should().Be(25m);
    }

    [Fact]
    public void CalculateMinimumPayment_UsesPercentWhenLargerThanFloor()
    {
        var min = CreditCardMath.CalculateMinimumPayment(
            balance: 5000m,
            percentagePoints: 2m,
            floor: 25m);

        min.Should().Be(100m);
    }

    [Fact]
    public void ResolveMinimumPayment_UsesFixedWhenPercentMissing()
    {
        var min = CreditCardMath.ResolveMinimumPayment(
            balance: 500m,
            fixedMonthlyPayment: 75m,
            minimumPaymentPercentage: null,
            minimumPaymentFloor: null);

        min.Should().Be(75m);
    }

    [Fact]
    public void MonthlyRate_ConvertsApr()
    {
        CreditCardMath.MonthlyRate(12m).Should().Be(0.01m);
    }
}
