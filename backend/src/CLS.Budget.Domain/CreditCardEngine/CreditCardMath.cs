namespace CLS.Budget.Domain.CreditCardEngine;

/// <summary>Shared money and APR helpers for credit card engines.</summary>
public static class CreditCardMath
{
    public const string FormulaVersion = "1.0";
    public const int MaxPayoffMonths = 1200;

    public static decimal MonthlyRate(decimal annualPercentageRate) =>
        annualPercentageRate / 100m / 12m;

    public static decimal DailyRate(decimal annualPercentageRate) =>
        annualPercentageRate / 100m / 365m;

    public static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Minimum payment using percent-of-balance (percent points, e.g. 2 = 2%) and a dollar floor.
    /// </summary>
    public static decimal CalculateMinimumPayment(
        decimal balance,
        decimal percentagePoints,
        decimal floor)
    {
        if (balance <= 0)
        {
            return 0m;
        }

        var percentageAmount = balance * (percentagePoints / 100m);
        return RoundMoney(Math.Min(balance, Math.Max(percentageAmount, floor)));
    }

    /// <summary>
    /// Resolves the payment minimum for a card: percentage+floor when both set, else fixed monthly payment.
    /// </summary>
    public static decimal ResolveMinimumPayment(
        decimal balance,
        decimal? fixedMonthlyPayment,
        decimal? minimumPaymentPercentage,
        decimal? minimumPaymentFloor)
    {
        if (minimumPaymentPercentage is not null && minimumPaymentFloor is not null)
        {
            return CalculateMinimumPayment(
                balance,
                minimumPaymentPercentage.Value,
                minimumPaymentFloor.Value);
        }

        if (fixedMonthlyPayment is null || fixedMonthlyPayment <= 0)
        {
            return 0m;
        }

        return RoundMoney(Math.Min(balance, fixedMonthlyPayment.Value));
    }

    public static decimal EffectiveApr(
        decimal standardApr,
        decimal? promotionalApr,
        DateOnly? promotionalExpiration,
        DateOnly asOf)
    {
        if (promotionalApr is null || promotionalExpiration is null)
        {
            return standardApr;
        }

        return asOf <= promotionalExpiration.Value
            ? promotionalApr.Value
            : standardApr;
    }
}
