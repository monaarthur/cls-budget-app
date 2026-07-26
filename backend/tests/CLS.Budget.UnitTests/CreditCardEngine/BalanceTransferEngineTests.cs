using CLS.Budget.Domain.CreditCardEngine.BalanceTransfer;
using FluentAssertions;

namespace CLS.Budget.UnitTests.CreditCardEngine;

public sealed class BalanceTransferEngineTests
{
    private readonly BalanceTransferEngine _sut = new();

    [Fact]
    public void Analyze_ZeroPercentOffer_ComputesFeeAndPaymentToClear()
    {
        var result = _sut.Analyze(new BalanceTransferAnalysisRequest(
            TransferAmount: 5000m,
            CurrentAnnualPercentageRate: 24m,
            PromotionalAnnualPercentageRate: 0m,
            PromotionalPeriodMonths: 12,
            TransferFeePercentage: 3m,
            TransferFeeFlatAmount: 0m,
            NewRegularAnnualPercentageRate: 22m,
            PlannedMonthlyPayment: 450m,
            AvailableTransferLimit: 10000m,
            StartDate: new DateOnly(2026, 1, 1)));

        result.IsValid.Should().BeTrue();
        result.TotalTransferFee.Should().Be(150m);
        result.StartingBalanceWithTransfer.Should().Be(5150m);
        result.PaymentNeededToClearBeforePromotionEnds.Should().Be(
            Math.Round(5150m / 12m, 2, MidpointRounding.AwayFromZero));
        result.InterestWithTransfer.Should().Be(0m);
        result.InterestWithoutTransfer.Should().BeGreaterThan(0m);
        result.NetSavings.Should().BeGreaterThan(0m);
        result.Recommendation.Should().Be(BalanceTransferRecommendation.Recommended);
    }

    [Fact]
    public void Analyze_CapsTransferAtAvailableLimit()
    {
        var result = _sut.Analyze(new BalanceTransferAnalysisRequest(
            TransferAmount: 5000m,
            CurrentAnnualPercentageRate: 20m,
            PromotionalAnnualPercentageRate: 0m,
            PromotionalPeriodMonths: 12,
            TransferFeePercentage: 0m,
            TransferFeeFlatAmount: 0m,
            NewRegularAnnualPercentageRate: 20m,
            PlannedMonthlyPayment: 500m,
            AvailableTransferLimit: 2000m,
            StartDate: new DateOnly(2026, 1, 1)));

        result.IsValid.Should().BeTrue();
        result.AppliedTransferAmount.Should().Be(2000m);
        result.Warnings.Should().Contain(w => w.Contains("capped", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_HighFee_NotRecommended()
    {
        var result = _sut.Analyze(new BalanceTransferAnalysisRequest(
            TransferAmount: 1000m,
            CurrentAnnualPercentageRate: 12m,
            PromotionalAnnualPercentageRate: 0m,
            PromotionalPeriodMonths: 3,
            TransferFeePercentage: 20m,
            TransferFeeFlatAmount: 50m,
            NewRegularAnnualPercentageRate: 22m,
            PlannedMonthlyPayment: 50m,
            AvailableTransferLimit: 5000m,
            StartDate: new DateOnly(2026, 1, 1)));

        result.IsValid.Should().BeTrue();
        result.NetSavings.Should().BeLessThanOrEqualTo(0m);
        result.Recommendation.Should().Be(BalanceTransferRecommendation.NotRecommended);
        result.Warnings.Should().Contain(w => w.Contains("fee", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_LowPayment_WarnsBalanceRemainsAtPromoEnd()
    {
        var result = _sut.Analyze(new BalanceTransferAnalysisRequest(
            TransferAmount: 3000m,
            CurrentAnnualPercentageRate: 22m,
            PromotionalAnnualPercentageRate: 0m,
            PromotionalPeriodMonths: 12,
            TransferFeePercentage: 3m,
            TransferFeeFlatAmount: 0m,
            NewRegularAnnualPercentageRate: 22m,
            PlannedMonthlyPayment: 50m,
            AvailableTransferLimit: 10000m,
            StartDate: new DateOnly(2026, 1, 1)));

        result.IsValid.Should().BeTrue();
        result.BalanceRemainingWhenPromotionEnds.Should().BeGreaterThan(0m);
        result.Recommendation.Should().BeOneOf(
            BalanceTransferRecommendation.PotentiallyBeneficial,
            BalanceTransferRecommendation.NotRecommended);
        result.Warnings.Should().Contain(w =>
            w.Contains("will not clear", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_InvalidInput_InsufficientInformation()
    {
        var result = _sut.Analyze(new BalanceTransferAnalysisRequest(
            TransferAmount: 0m,
            CurrentAnnualPercentageRate: 20m,
            PromotionalAnnualPercentageRate: 0m,
            PromotionalPeriodMonths: 12,
            TransferFeePercentage: 3m,
            TransferFeeFlatAmount: 0m,
            NewRegularAnnualPercentageRate: 20m,
            PlannedMonthlyPayment: 100m,
            AvailableTransferLimit: 5000m,
            StartDate: new DateOnly(2026, 1, 1)));

        result.IsValid.Should().BeFalse();
        result.Recommendation.Should().Be(BalanceTransferRecommendation.InsufficientInformation);
    }
}
