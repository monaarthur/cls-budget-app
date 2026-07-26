using CLS.Budget.Domain.CreditCardEngine.Payoff;

namespace CLS.Budget.Domain.CreditCardEngine.Forecast;

public sealed class ForecastEngine(IPayoffStrategyEngine payoffEngine) : IForecastEngine
{
    public const int MinForecastMonths = 1;
    public const int MaxForecastMonths = 1200;
    public const int RecommendedMinSupportedMonths = 120;

    public ForecastResult Generate(ForecastRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.CreditCards);
        ArgumentNullException.ThrowIfNull(payoffEngine);

        var warnings = new List<string>();
        if (request.ForecastMonths < MinForecastMonths || request.ForecastMonths > MaxForecastMonths)
        {
            return Invalid(
                request,
                [$"Forecast duration must be between {MinForecastMonths} and {MaxForecastMonths} months."]);
        }

        if (request.TotalMonthlyDebtPayment < 0
            || request.MonthlyNetIncome < 0
            || request.MonthlyExpenses < 0)
        {
            return Invalid(request, ["Income, expenses, and monthly payment cannot be negative."]);
        }

        var states = request.CreditCards
            .Select(c => new CardState
            {
                Input = c,
                Balance = CreditCardMath.RoundMoney(Math.Max(0m, c.CurrentBalance)),
                CreditLimit = Math.Max(0m, c.CreditLimit)
            })
            .ToList();

        var startingDebt = CreditCardMath.RoundMoney(states.Sum(c => c.Balance));
        if (states.Count == 0 || startingDebt <= 0)
        {
            warnings.Add("No credit card balances to forecast.");
        }

        var charges = request.AdditionalCharges ?? [];
        var oneTimePayments = request.OneTimePayments ?? [];
        var paymentOverrides = request.PaymentOverrides ?? [];
        var incomeChanges = request.IncomeChanges ?? [];
        var expenseChanges = request.ExpenseChanges ?? [];

        var months = new List<ForecastMonthSnapshot>();
        var cumulativeInterest = 0m;
        var monthDate = request.StartDate;
        DateOnly? debtFreeDate = null;
        var debtIncreased = false;

        for (var index = 0; index < request.ForecastMonths; index++)
        {
            var startingDebtMonth = CreditCardMath.RoundMoney(states.Sum(c => c.Balance));
            var newCharges = ApplyCharges(states, charges, index);
            var paymentBudget = ResolvePaymentBudget(
                request.TotalMonthlyDebtPayment,
                paymentOverrides,
                oneTimePayments,
                index);

            var activeInputs = states
                .Where(c => c.Balance > 0)
                .Select(c => c.Input with { CurrentBalance = c.Balance })
                .ToList();

            decimal interest = 0;
            decimal payments = 0;
            var paidOffThisMonth = 0;

            if (activeInputs.Count > 0)
            {
                var plan = payoffEngine.GeneratePlan(new PayoffPlanRequest(
                    activeInputs,
                    paymentBudget,
                    request.Strategy,
                    monthDate,
                    request.TargetUtilizationPercent,
                    request.PayOverLimitFirst));

                if (!plan.IsValid)
                {
                    warnings.AddRange(plan.Warnings);
                    warnings.Add($"Forecast stopped in month {index + 1}: payment budget is below required minimums.");
                    months.Add(BuildSnapshot(
                        monthDate,
                        index,
                        startingDebtMonth,
                        newCharges,
                        interest: 0,
                        payments: 0,
                        endingDebt: startingDebtMonth,
                        states,
                        availableCash: ResolveAvailableCash(
                            request, incomeChanges, expenseChanges, index, payments: 0),
                        cardsPaidOff: 0,
                        cumulativeInterest));
                    break;
                }

                var monthItems = plan.Schedule.Where(s => s.Month == monthDate).ToList();
                interest = CreditCardMath.RoundMoney(monthItems.Sum(s => s.InterestCharged));
                payments = CreditCardMath.RoundMoney(monthItems.Sum(s => s.PaymentApplied));

                foreach (var item in monthItems)
                {
                    var state = states.First(c => c.Input.CreditCardId == item.CreditCardId);
                    var wasPositive = state.Balance > 0;
                    state.Balance = item.EndingBalance;
                    if (wasPositive && state.Balance <= 0)
                    {
                        paidOffThisMonth++;
                    }
                }
            }

            var endingDebt = CreditCardMath.RoundMoney(states.Sum(c => c.Balance));
            cumulativeInterest = CreditCardMath.RoundMoney(cumulativeInterest + interest);

            if (endingDebt > startingDebtMonth + 0.009m)
            {
                debtIncreased = true;
            }

            if (endingDebt <= 0 && debtFreeDate is null && startingDebtMonth > 0)
            {
                debtFreeDate = monthDate.AddMonths(1);
            }

            var availableCash = ResolveAvailableCash(
                request, incomeChanges, expenseChanges, index, payments);

            months.Add(BuildSnapshot(
                monthDate,
                index,
                startingDebtMonth,
                newCharges,
                interest,
                payments,
                endingDebt,
                states,
                availableCash,
                paidOffThisMonth,
                cumulativeInterest));

            monthDate = monthDate.AddMonths(1);

            if (endingDebt <= 0
                && !HasFutureCharges(charges, index)
                && index + 1 < request.ForecastMonths)
            {
                // Fill remaining months as zero-debt for a stable horizon.
                for (var fill = index + 1; fill < request.ForecastMonths; fill++)
                {
                    var fillPayments = 0m;
                    var fillCash = ResolveAvailableCash(
                        request, incomeChanges, expenseChanges, fill, fillPayments);
                    months.Add(BuildSnapshot(
                        monthDate,
                        fill,
                        startingDebt: 0,
                        newCharges: 0,
                        interest: 0,
                        payments: 0,
                        endingDebt: 0,
                        states,
                        fillCash,
                        cardsPaidOff: 0,
                        cumulativeInterest));
                    monthDate = monthDate.AddMonths(1);
                }

                break;
            }
        }

        if (debtIncreased)
        {
            warnings.Add("Debt increased during one or more forecast months (new charges and/or interest exceeded payments).");
        }

        return new ForecastResult(
            Strategy: request.Strategy,
            StartingDebt: startingDebt,
            TotalMonthlyDebtPayment: request.TotalMonthlyDebtPayment,
            ForecastMonths: request.ForecastMonths,
            EstimatedDebtFreeDate: debtFreeDate,
            TotalInterestPaid: cumulativeInterest,
            Months: months,
            Warnings: warnings,
            IsValid: true);
    }

    private static ForecastResult Invalid(ForecastRequest request, IReadOnlyList<string> warnings) =>
        new(
            Strategy: request.Strategy,
            StartingDebt: 0,
            TotalMonthlyDebtPayment: request.TotalMonthlyDebtPayment,
            ForecastMonths: request.ForecastMonths,
            EstimatedDebtFreeDate: null,
            TotalInterestPaid: 0,
            Months: [],
            Warnings: warnings,
            IsValid: false);

    private static decimal ApplyCharges(
        List<CardState> states,
        IReadOnlyList<ForecastCharge> charges,
        int monthIndex)
    {
        var total = 0m;
        foreach (var charge in charges.Where(c => c.MonthOffset == monthIndex && c.Amount > 0))
        {
            CardState? target = null;
            if (charge.CreditCardId is not null)
            {
                target = states.FirstOrDefault(c => c.Input.CreditCardId == charge.CreditCardId);
            }

            target ??= states
                .OrderByDescending(c => c.Input.AnnualPercentageRate)
                .ThenByDescending(c => c.Balance)
                .FirstOrDefault();

            if (target is null)
            {
                continue;
            }

            var amount = CreditCardMath.RoundMoney(charge.Amount);
            target.Balance = CreditCardMath.RoundMoney(target.Balance + amount);
            total = CreditCardMath.RoundMoney(total + amount);
        }

        return total;
    }

    private static bool HasFutureCharges(IReadOnlyList<ForecastCharge> charges, int monthIndex) =>
        charges.Any(c => c.MonthOffset > monthIndex && c.Amount > 0);

    private static decimal ResolvePaymentBudget(
        decimal basePayment,
        IReadOnlyList<ForecastPaymentOverride> overrides,
        IReadOnlyList<ForecastOneTimePayment> oneTimePayments,
        int monthIndex)
    {
        var overridePayment = overrides
            .Where(o => o.MonthOffset == monthIndex)
            .Select(o => (decimal?)o.TotalMonthlyDebtPayment)
            .LastOrDefault();
        var payment = overridePayment ?? basePayment;
        var bonus = oneTimePayments
            .Where(p => p.MonthOffset == monthIndex)
            .Sum(p => p.Amount);
        return CreditCardMath.RoundMoney(Math.Max(0m, payment + bonus));
    }

    private static decimal ResolveAvailableCash(
        ForecastRequest request,
        IReadOnlyList<ForecastIncomeChange> incomeChanges,
        IReadOnlyList<ForecastExpenseChange> expenseChanges,
        int monthIndex,
        decimal payments)
    {
        var income = request.MonthlyNetIncome
            + incomeChanges.Where(c => c.MonthOffset <= monthIndex).Sum(c => c.MonthlyNetIncomeDelta);
        var expenses = request.MonthlyExpenses
            + expenseChanges.Where(c => c.MonthOffset <= monthIndex).Sum(c => c.MonthlyExpenseDelta);
        return CreditCardMath.RoundMoney(income - expenses - payments);
    }

    private static ForecastMonthSnapshot BuildSnapshot(
        DateOnly month,
        int monthIndex,
        decimal startingDebt,
        decimal newCharges,
        decimal interest,
        decimal payments,
        decimal endingDebt,
        List<CardState> states,
        decimal availableCash,
        int cardsPaidOff,
        decimal cumulativeInterest)
    {
        var totalLimit = CreditCardMath.RoundMoney(states.Sum(c => c.CreditLimit));
        var utilization = totalLimit <= 0
            ? 0m
            : CreditCardMath.RoundMoney(endingDebt / totalLimit * 100m);

        return new ForecastMonthSnapshot(
            Month: month,
            MonthIndex: monthIndex,
            StartingDebt: startingDebt,
            NewCharges: newCharges,
            Interest: interest,
            Payments: payments,
            EndingDebt: endingDebt,
            TotalCreditLimit: totalLimit,
            OverallUtilizationPercentage: utilization,
            AvailableCash: availableCash,
            CardsPaidOffThisMonth: cardsPaidOff,
            CumulativeInterest: cumulativeInterest);
    }

    private sealed class CardState
    {
        public required CreditCardPayoffInput Input { get; init; }
        public decimal Balance { get; set; }
        public decimal CreditLimit { get; init; }
    }
}
