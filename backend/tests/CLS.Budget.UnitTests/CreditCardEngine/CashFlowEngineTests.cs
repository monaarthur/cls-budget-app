using CLS.Budget.Domain.CreditCardEngine.CashFlow;
using FluentAssertions;

namespace CLS.Budget.UnitTests.CreditCardEngine;

public sealed class CashFlowEngineTests
{
    private readonly CashFlowEngine _sut = new();

    [Fact]
    public void Calculate_SafeExtraLeavesSafetyBuffer()
    {
        var result = _sut.Calculate(new CashFlowRequest(
            MonthlyNetIncome: 5000m,
            RequiredExpenses: 2000m,
            VariableExpenses: 500m,
            ExistingDebtMinimums: 400m,
            EmergencySavingsContribution: 200m,
            SafetyBuffer: 300m));

        // Disposable = 5000 - 2000 - 500 - 400 - 200 = 1900
        result.IsValid.Should().BeTrue();
        result.MonthlyDisposableIncome.Should().Be(1900m);
        result.SafeExtraDebtPayment.Should().Be(1600m);
        result.AggressiveExtraDebtPayment.Should().Be(1900m);
        result.RemainingCashBuffer.Should().Be(300m);
        result.RecommendedExtraDebtPayment.Should().Be(1600m);
        result.UsedUserOverride.Should().BeFalse();
    }

    [Fact]
    public void Calculate_NeverRecommendsNegativePayment()
    {
        var result = _sut.Calculate(new CashFlowRequest(
            MonthlyNetIncome: 1000m,
            RequiredExpenses: 1200m,
            VariableExpenses: 200m,
            ExistingDebtMinimums: 300m,
            EmergencySavingsContribution: 0m,
            SafetyBuffer: 100m));

        result.IsValid.Should().BeTrue();
        result.SafeExtraDebtPayment.Should().Be(0m);
        result.AggressiveExtraDebtPayment.Should().Be(0m);
        result.RecommendedExtraDebtPayment.Should().Be(0m);
        result.Warnings.Should().Contain(w => w.Contains("exceed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Calculate_IncludesAdditionalAvailableFunds()
    {
        var result = _sut.Calculate(new CashFlowRequest(
            MonthlyNetIncome: 3000m,
            RequiredExpenses: 1500m,
            VariableExpenses: 500m,
            ExistingDebtMinimums: 200m,
            EmergencySavingsContribution: 100m,
            SafetyBuffer: 200m,
            AdditionalAvailableFunds: 400m));

        // Disposable = 3000 + 400 - 1500 - 500 - 200 - 100 = 1100
        result.MonthlyDisposableIncome.Should().Be(1100m);
        result.SafeExtraDebtPayment.Should().Be(900m);
    }

    [Fact]
    public void Calculate_UserOverrideIsHonored()
    {
        var result = _sut.Calculate(new CashFlowRequest(
            MonthlyNetIncome: 4000m,
            RequiredExpenses: 1500m,
            VariableExpenses: 500m,
            ExistingDebtMinimums: 300m,
            EmergencySavingsContribution: 200m,
            SafetyBuffer: 250m,
            UserOverrideExtraPayment: 1000m));

        result.UsedUserOverride.Should().BeTrue();
        result.RecommendedExtraDebtPayment.Should().Be(1000m);
        result.SafeExtraDebtPayment.Should().Be(1250m);
    }

    [Fact]
    public void Calculate_NegativeInput_Invalid()
    {
        var result = _sut.Calculate(new CashFlowRequest(
            MonthlyNetIncome: -1m,
            RequiredExpenses: 0m,
            VariableExpenses: 0m,
            ExistingDebtMinimums: 0m,
            EmergencySavingsContribution: 0m,
            SafetyBuffer: 0m));

        result.IsValid.Should().BeFalse();
        result.RecommendedExtraDebtPayment.Should().Be(0m);
    }
}
