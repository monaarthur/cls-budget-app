namespace CLS.Budget.Domain.CreditCardEngine.CashFlow;

public sealed class CashFlowEngine : ICashFlowEngine
{
    public CashFlowResult Calculate(CashFlowRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var warnings = new List<string>();

        if (request.MonthlyNetIncome < 0
            || request.RequiredExpenses < 0
            || request.VariableExpenses < 0
            || request.ExistingDebtMinimums < 0
            || request.EmergencySavingsContribution < 0
            || request.SafetyBuffer < 0
            || request.AdditionalAvailableFunds < 0
            || request.UserOverrideExtraPayment is < 0)
        {
            return new CashFlowResult(
                MonthlyDisposableIncome: 0,
                RequiredDebtMinimums: CreditCardMath.RoundMoney(Math.Max(0m, request.ExistingDebtMinimums)),
                SafeExtraDebtPayment: 0,
                AggressiveExtraDebtPayment: 0,
                RemainingCashBuffer: 0,
                RecommendedExtraDebtPayment: 0,
                UsedUserOverride: false,
                Warnings: ["All cash-flow inputs must be zero or greater."],
                IsValid: false);
        }

        var disposable = CreditCardMath.RoundMoney(
            request.MonthlyNetIncome
            + request.AdditionalAvailableFunds
            - request.RequiredExpenses
            - request.VariableExpenses
            - request.ExistingDebtMinimums
            - request.EmergencySavingsContribution);

        if (disposable < 0)
        {
            warnings.Add("Expenses, debt minimums, and savings exceed monthly net income.");
        }

        var safeExtra = CreditCardMath.RoundMoney(Math.Max(0m, disposable - request.SafetyBuffer));
        var aggressiveExtra = CreditCardMath.RoundMoney(Math.Max(0m, disposable));
        var remainingBuffer = CreditCardMath.RoundMoney(Math.Max(0m, disposable - safeExtra));

        var usedOverride = false;
        var recommended = safeExtra;
        if (request.UserOverrideExtraPayment is not null)
        {
            usedOverride = true;
            recommended = CreditCardMath.RoundMoney(Math.Max(0m, request.UserOverrideExtraPayment.Value));
            if (recommended > aggressiveExtra)
            {
                warnings.Add(
                    "The override extra payment exceeds estimated disposable income and may not be sustainable.");
            }
            else if (recommended > safeExtra)
            {
                warnings.Add(
                    "The override extra payment uses part of the safety buffer.");
            }
        }

        if (safeExtra == 0 && disposable <= 0)
        {
            warnings.Add("No safe extra debt payment is available with the current budget.");
        }

        if (request.ExistingDebtMinimums == 0)
        {
            warnings.Add("Debt minimums are zero; confirm minimum payments are entered correctly.");
        }

        return new CashFlowResult(
            MonthlyDisposableIncome: disposable,
            RequiredDebtMinimums: CreditCardMath.RoundMoney(request.ExistingDebtMinimums),
            SafeExtraDebtPayment: safeExtra,
            AggressiveExtraDebtPayment: aggressiveExtra,
            RemainingCashBuffer: remainingBuffer,
            RecommendedExtraDebtPayment: recommended,
            UsedUserOverride: usedOverride,
            Warnings: warnings,
            IsValid: true);
    }
}
