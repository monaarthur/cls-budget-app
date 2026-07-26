namespace CLS.Budget.Domain.CreditCardEngine.Interest;

public sealed record InterestCalculationRequest(
    decimal Balance,
    decimal AnnualPercentageRate,
    decimal MonthlyPayment,
    DateOnly StartDate,
    decimal? PromotionalAnnualPercentageRate = null,
    DateOnly? PromotionalRateExpirationDate = null);

public sealed record InterestCalculationResult(
    decimal DailyInterest,
    decimal EstimatedMonthlyInterest,
    decimal EstimatedAnnualInterest,
    decimal TotalInterestPaid,
    decimal TotalPrincipalPaid,
    decimal RemainingBalance,
    DateOnly? EstimatedPayoffDate,
    int NumberOfPayments,
    bool NegativeAmortizationDetected);
