using System.Text.Json.Serialization;
using CLS.Budget.Application.Common.Serialization;
using CLS.Budget.Domain.CreditCardEngine.BalanceTransfer;

namespace CLS.Budget.Application.CreditCardEngine.Dtos;

public sealed class AnalyzeBalanceTransferRequest
{
    public decimal TransferAmount { get; init; }
    public decimal CurrentAnnualPercentageRate { get; init; }
    public decimal PromotionalAnnualPercentageRate { get; init; }
    public int PromotionalPeriodMonths { get; init; }
    public decimal TransferFeePercentage { get; init; }
    public decimal TransferFeeFlatAmount { get; init; }
    public decimal NewRegularAnnualPercentageRate { get; init; }
    public decimal PlannedMonthlyPayment { get; init; }
    public decimal AvailableTransferLimit { get; init; }

    [JsonConverter(typeof(NullableDateOnlyUtcJsonConverter))]
    public DateTime? StartDate { get; init; }

    public bool IncludeFeeInTransferredBalance { get; init; } = true;
}

public sealed class BalanceTransferAnalysisResultDto
{
    public decimal RequestedTransferAmount { get; init; }
    public decimal AppliedTransferAmount { get; init; }
    public decimal TotalTransferFee { get; init; }
    public decimal StartingBalanceWithTransfer { get; init; }
    public decimal InterestWithoutTransfer { get; init; }
    public decimal InterestWithTransfer { get; init; }
    public decimal NetSavings { get; init; }
    public int? BreakEvenMonth { get; init; }
    public decimal BalanceRemainingWhenPromotionEnds { get; init; }
    public decimal PaymentNeededToClearBeforePromotionEnds { get; init; }
    public int MonthsCompared { get; init; }
    public string Recommendation { get; init; } = null!;
    public string Explanation { get; init; } = null!;
}

internal static class BalanceTransferRecommendationNames
{
    public static string ToDisplayName(BalanceTransferRecommendation value) => value switch
    {
        BalanceTransferRecommendation.Recommended => "Recommended",
        BalanceTransferRecommendation.PotentiallyBeneficial => "PotentiallyBeneficial",
        BalanceTransferRecommendation.NotRecommended => "NotRecommended",
        BalanceTransferRecommendation.InsufficientInformation => "InsufficientInformation",
        _ => value.ToString()
    };
}
