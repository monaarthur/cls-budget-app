namespace CLS.Budget.Domain.CreditCardEngine.Utilization;

public sealed record CreditCardUtilizationInput(
    int CreditCardId,
    string Name,
    decimal CurrentBalance,
    decimal CreditLimit);

public sealed record UtilizationThresholdTarget(
    decimal ThresholdPercent,
    decimal TargetBalance,
    decimal PaymentRequired);

public sealed record CardUtilizationResult(
    int CreditCardId,
    string Name,
    decimal CurrentBalance,
    decimal CreditLimit,
    decimal AvailableCredit,
    decimal UtilizationPercentage,
    IReadOnlyList<UtilizationThresholdTarget> ThresholdTargets);

public sealed record UtilizationResult(
    IReadOnlyList<CardUtilizationResult> Cards,
    decimal TotalBalances,
    decimal TotalCreditLimits,
    decimal OverallUtilizationPercentage,
    IReadOnlyList<UtilizationThresholdTarget> OverallThresholdTargets);
