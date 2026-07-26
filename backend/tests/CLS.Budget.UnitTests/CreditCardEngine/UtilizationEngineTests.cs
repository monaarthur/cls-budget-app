using CLS.Budget.Domain.CreditCardEngine.Utilization;
using FluentAssertions;

namespace CLS.Budget.UnitTests.CreditCardEngine;

public sealed class UtilizationEngineTests
{
    private readonly UtilizationEngine _sut = new();

    [Fact]
    public void Calculate_ComputesPerCardAndOverallUtilization()
    {
        var result = _sut.Calculate(
        [
            new CreditCardUtilizationInput(1, "A", 900m, 1000m),
            new CreditCardUtilizationInput(2, "B", 100m, 1000m),
        ]);

        result.OverallUtilizationPercentage.Should().Be(50m);
        result.TotalBalances.Should().Be(1000m);
        result.TotalCreditLimits.Should().Be(2000m);
        result.Cards.Should().HaveCount(2);
        result.Cards[0].Name.Should().Be("A");
        result.Cards[0].UtilizationPercentage.Should().Be(90m);
    }

    [Fact]
    public void Calculate_ZeroLimit_IsSafeAndReportsZeroUtilization()
    {
        var result = _sut.Calculate(
        [
            new CreditCardUtilizationInput(1, "No Limit", 250m, 0m),
        ]);

        result.Cards.Should().ContainSingle();
        result.Cards[0].UtilizationPercentage.Should().Be(0m);
        result.Cards[0].AvailableCredit.Should().Be(0m);
        result.OverallUtilizationPercentage.Should().Be(0m);
    }

    [Fact]
    public void Calculate_IncludesDefaultThresholdTargets()
    {
        var result = _sut.Calculate(
        [
            new CreditCardUtilizationInput(1, "Card", 800m, 1000m),
        ]);

        result.Cards[0].ThresholdTargets.Select(t => t.ThresholdPercent)
            .Should().Equal(90m, 70m, 50m, 30m, 10m);

        var toFifty = result.Cards[0].ThresholdTargets.Single(t => t.ThresholdPercent == 50m);
        toFifty.TargetBalance.Should().Be(500m);
        toFifty.PaymentRequired.Should().Be(300m);
    }
}
