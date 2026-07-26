namespace CLS.Budget.Domain.CreditCardEngine.Utilization;

public sealed class UtilizationEngine : IUtilizationEngine
{
    public static readonly decimal[] DefaultThresholds = [90m, 70m, 50m, 30m, 10m];

    public UtilizationResult Calculate(IReadOnlyCollection<CreditCardUtilizationInput> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        var cardResults = cards
            .Select(CalculateCard)
            .OrderByDescending(c => c.UtilizationPercentage)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var totalBalances = CreditCardMath.RoundMoney(cardResults.Sum(c => c.CurrentBalance));
        var totalLimits = CreditCardMath.RoundMoney(cardResults.Sum(c => c.CreditLimit));
        var overallUtilization = totalLimits <= 0
            ? 0m
            : CreditCardMath.RoundMoney(totalBalances / totalLimits * 100m);

        return new UtilizationResult(
            Cards: cardResults,
            TotalBalances: totalBalances,
            TotalCreditLimits: totalLimits,
            OverallUtilizationPercentage: overallUtilization,
            OverallThresholdTargets: BuildThresholds(totalBalances, totalLimits));
    }

    private static CardUtilizationResult CalculateCard(CreditCardUtilizationInput card)
    {
        var balance = Math.Max(0m, card.CurrentBalance);
        var limit = Math.Max(0m, card.CreditLimit);
        var available = CreditCardMath.RoundMoney(Math.Max(0m, limit - balance));
        var utilization = limit <= 0
            ? 0m
            : CreditCardMath.RoundMoney(balance / limit * 100m);

        return new CardUtilizationResult(
            CreditCardId: card.CreditCardId,
            Name: card.Name,
            CurrentBalance: CreditCardMath.RoundMoney(balance),
            CreditLimit: CreditCardMath.RoundMoney(limit),
            AvailableCredit: available,
            UtilizationPercentage: utilization,
            ThresholdTargets: BuildThresholds(balance, limit));
    }

    private static IReadOnlyList<UtilizationThresholdTarget> BuildThresholds(
        decimal balance,
        decimal limit)
    {
        return DefaultThresholds
            .Select(threshold =>
            {
                var targetBalance = CreditCardMath.RoundMoney(limit * (threshold / 100m));
                var paymentRequired = CreditCardMath.RoundMoney(Math.Max(0m, balance - targetBalance));
                return new UtilizationThresholdTarget(threshold, targetBalance, paymentRequired);
            })
            .ToList();
    }
}
