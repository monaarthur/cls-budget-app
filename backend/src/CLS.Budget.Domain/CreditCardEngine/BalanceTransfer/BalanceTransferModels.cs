namespace CLS.Budget.Domain.CreditCardEngine.BalanceTransfer;

public enum BalanceTransferRecommendation
{
    Recommended,
    PotentiallyBeneficial,
    NotRecommended,
    InsufficientInformation
}

public sealed record BalanceTransferAnalysisRequest(
    decimal TransferAmount,
    decimal CurrentAnnualPercentageRate,
    decimal PromotionalAnnualPercentageRate,
    int PromotionalPeriodMonths,
    decimal TransferFeePercentage,
    decimal TransferFeeFlatAmount,
    decimal NewRegularAnnualPercentageRate,
    decimal PlannedMonthlyPayment,
    decimal AvailableTransferLimit,
    DateOnly StartDate,
    bool IncludeFeeInTransferredBalance = true);

public sealed record BalanceTransferAnalysisResult(
    decimal RequestedTransferAmount,
    decimal AppliedTransferAmount,
    decimal TotalTransferFee,
    decimal StartingBalanceWithTransfer,
    decimal InterestWithoutTransfer,
    decimal InterestWithTransfer,
    decimal NetSavings,
    int? BreakEvenMonth,
    decimal BalanceRemainingWhenPromotionEnds,
    decimal PaymentNeededToClearBeforePromotionEnds,
    int MonthsCompared,
    BalanceTransferRecommendation Recommendation,
    string Explanation,
    IReadOnlyList<string> Warnings,
    bool IsValid);
