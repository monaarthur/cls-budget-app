namespace CLS.Budget.Domain.CreditCardEngine.Payoff;

using CLS.Budget.Domain.CreditCardEngine.Loan;

public sealed class PayoffStrategyEngine : IPayoffStrategyEngine
{
    /// <summary>Synthetic debt id for an optional loan included in the plan.</summary>
    public const int LoanCreditCardId = -1;

    private sealed class CardState
    {
        public required CreditCardPayoffInput Input { get; init; }
        public decimal PurchaseBalance { get; set; }
        public decimal CashAdvanceBalance { get; set; }
        public decimal PromoBalance { get; set; }
        public decimal? ActivePromoApr { get; set; }
        public DateOnly? PromoExpiresOn { get; set; }
        public decimal TotalInterest { get; set; }
        public int MonthsToPayoff { get; set; }
        public DateOnly? PayoffDate { get; set; }
        /// <summary>HELOC: months remaining in interest-only phase.</summary>
        public int LoanInterestOnlyMonthsRemaining { get; set; }
        /// <summary>HELOC: amortizing payment after interest-only phase.</summary>
        public decimal? LoanPhase2MonthlyPayment { get; set; }
        public decimal Balance =>
            CreditCardMath.RoundMoney(PurchaseBalance + CashAdvanceBalance + PromoBalance);
        public bool IsPaidOff => Balance <= 0;
        public bool IsLoan => Input.CreditCardId == LoanCreditCardId;

        public void ClearBalances()
        {
            PurchaseBalance = 0;
            CashAdvanceBalance = 0;
            PromoBalance = 0;
            ActivePromoApr = null;
            PromoExpiresOn = null;
        }
    }

    public PayoffPlanResult GeneratePlan(PayoffPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.CreditCards);

        var targetUtil = NormalizeTargetUtilization(request.TargetUtilizationPercent);
        var warnings = new List<string>();
        var cards = request.CreditCards
            .Where(c => c.CurrentBalance > 0)
            .Select(c => new CardState
            {
                Input = c,
                PurchaseBalance = CreditCardMath.RoundMoney(c.CurrentBalance),
                CashAdvanceBalance = 0
            })
            .ToList();

        ApplyOptionalLoan(request, cards, warnings);

        var startingDebt = CreditCardMath.RoundMoney(cards.Sum(c => c.Balance));
        if (cards.Count == 0 || startingDebt <= 0)
        {
            return new PayoffPlanResult(
                Strategy: request.Strategy,
                StartingDebt: 0,
                TotalMonthlyDebtPayment: request.TotalMonthlyDebtPayment,
                CombinedMinimumPayments: 0,
                OverallDebtFreeDate: request.StartDate,
                MonthsToPayoff: 0,
                TotalInterestPaid: 0,
                TotalPrincipalPaid: 0,
                InterestSavedVersusMinimums: 0,
                CardOrder: [],
                Schedule: [],
                Warnings: ["No credit card balances to pay off."],
                IsValid: true);
        }

        var initialMins = cards.Sum(c => ResolveMin(c));
        if (request.Strategy != PayoffStrategyType.MinimumsOnly
            && request.TotalMonthlyDebtPayment + 0.0001m < initialMins)
        {
            return new PayoffPlanResult(
                Strategy: request.Strategy,
                StartingDebt: startingDebt,
                TotalMonthlyDebtPayment: request.TotalMonthlyDebtPayment,
                CombinedMinimumPayments: CreditCardMath.RoundMoney(initialMins),
                OverallDebtFreeDate: null,
                MonthsToPayoff: 0,
                TotalInterestPaid: 0,
                TotalPrincipalPaid: 0,
                InterestSavedVersusMinimums: null,
                CardOrder: [],
                Schedule: [],
                Warnings:
                [
                    $"Total monthly debt payment ({request.TotalMonthlyDebtPayment:C}) is below combined minimum payments ({initialMins:C})."
                ],
                IsValid: false);
        }

        if (request.PayOverLimitFirst
            && request.Strategy is PayoffStrategyType.Avalanche or PayoffStrategyType.Snowball)
        {
            warnings.Add(
                "Extra payments first bring over-limit balances back to the credit limit, then continue with the selected payoff strategy.");
        }

        if (targetUtil is not null
            && request.Strategy is PayoffStrategyType.Avalanche or PayoffStrategyType.Snowball)
        {
            var phase2 = request.PostUtilizationStrategy is PayoffStrategyType.Avalanche or PayoffStrategyType.Snowball
                ? request.PostUtilizationStrategy.Value
                : request.Strategy;
            warnings.Add(
                $"Extra payments first bring each card down to {targetUtil:0.##}% utilization using {request.Strategy}, then continue paying remaining balances to zero using {phase2}.");
        }

        if (request.EnableCashAdvanceBalanceMoves
            && request.Strategy is PayoffStrategyType.Avalanche or PayoffStrategyType.Snowball)
        {
            warnings.Add(
                "After each month's payments, if another card has a cash-advance APR lower than the card being paid, its available credit may be used to pay that card. If none qualify, the highest-APR card with available credit is used instead.");
        }

        var promoPlans = (request.PromotionalTransfers ?? [])
            .Where(t =>
                t.FromCreditCardId != t.ToCreditCardId
                && t.PromotionalPeriodMonths > 0
                && t.ApplyAtMonthOffset >= 0)
            .ToList();
        if (promoPlans.Count > 0
            && request.Strategy is PayoffStrategyType.Avalanche or PayoffStrategyType.Snowball)
        {
            warnings.Add(
                $"{promoPlans.Count} promotional APR balance transfer(s) are applied during the plan at their scheduled month(s).");
        }

        var schedule = new List<MonthlyPayoffScheduleItem>();
        var month = request.StartDate;
        var months = 0;
        var totalInterest = 0m;
        var totalPrincipal = 0m;
        var prioritySnapshot = OrderTargets(
                cards,
                request.Strategy,
                request.StartDate,
                targetUtil,
                request.PayOverLimitFirst)
            .Select((c, index) => (c.Input.CreditCardId, Order: index + 1))
            .ToDictionary(x => x.CreditCardId, x => x.Order);

        while (cards.Any(c => !c.IsPaidOff) && months < CreditCardMath.MaxPayoffMonths)
        {
            months++;
            var monthIndex = months - 1;

            var transferLegsByCard = cards.ToDictionary(
                c => c.Input.CreditCardId,
                _ => new List<BalanceTransferLeg>());
            var transferInByCard = cards.ToDictionary(c => c.Input.CreditCardId, _ => 0m);
            var transferOutByCard = cards.ToDictionary(c => c.Input.CreditCardId, _ => 0m);

            if (promoPlans.Count > 0
                && request.Strategy is PayoffStrategyType.Avalanche or PayoffStrategyType.Snowball)
            {
                ApplyPromotionalTransfers(
                    promoPlans.Where(p => p.ApplyAtMonthOffset == monthIndex).ToList(),
                    cards,
                    month,
                    transferLegsByCard,
                    transferInByCard,
                    transferOutByCard);
            }

            var active = cards.Where(c => !c.IsPaidOff).ToList();
            if (active.Count == 0)
            {
                break;
            }

            var overLimitPhase = request.PayOverLimitFirst
                && active.Any(IsOverLimit);
            // Phase 1: bring cards to the utilization target. Phase 2 (after all meet): pay to $0.
            var utilizationPhase = !overLimitPhase
                && targetUtil is not null
                && active.Any(c => IsAboveUtilizationTarget(c, targetUtil.Value));
            var effectiveStrategy = ResolveEffectiveStrategy(
                request.Strategy,
                request.PostUtilizationStrategy,
                targetUtil,
                utilizationPhase);

            var minsByCard = active.ToDictionary(
                c => c.Input.CreditCardId,
                c =>
                {
                    var min = ResolveMin(c);
                    if (!utilizationPhase
                        || targetUtil is null
                        || !IsAboveUtilizationTarget(c, targetUtil.Value))
                    {
                        return min;
                    }

                    // Cap the minimum so we only reach the utilization target in phase 1.
                    var maxPayment = MaxPaymentForTarget(
                        c,
                        month,
                        targetUtil,
                        overLimitPhase: false,
                        utilizationPhase: true);
                    return CreditCardMath.RoundMoney(Math.Min(min, maxPayment));
                });

            var requiredMins = minsByCard.Values.Sum();
            var budget = request.Strategy == PayoffStrategyType.MinimumsOnly
                ? requiredMins
                : request.TotalMonthlyDebtPayment;

            if (budget + 0.0001m < requiredMins)
            {
                warnings.Add($"In month {months}, payment budget fell below required minimums.");
                break;
            }

            var allocations = active.ToDictionary(
                c => c.Input.CreditCardId,
                c => minsByCard[c.Input.CreditCardId]);

            var extra = CreditCardMath.RoundMoney(budget - requiredMins);
            var targets = OrderTargets(
                active,
                effectiveStrategy,
                month,
                utilizationPhase ? targetUtil : null,
                request.PayOverLimitFirst);
            foreach (var target in targets)
            {
                if (extra <= 0)
                {
                    break;
                }

                var already = allocations[target.Input.CreditCardId];
                var maxPayment = MaxPaymentForTarget(
                    target,
                    month,
                    targetUtil,
                    overLimitPhase,
                    utilizationPhase);
                var room = CreditCardMath.RoundMoney(Math.Max(0m, maxPayment - already));
                var apply = Math.Min(extra, room);
                allocations[target.Input.CreditCardId] =
                    CreditCardMath.RoundMoney(allocations[target.Input.CreditCardId] + apply);
                extra = CreditCardMath.RoundMoney(extra - apply);
            }

            var monthRows = new List<(
                CardState Card,
                decimal Starting,
                decimal Interest,
                decimal Payment,
                decimal MinimumApplied,
                decimal ExtraApplied,
                decimal Principal)>();

            foreach (var card in active)
            {
                var starting = card.Balance;
                var interest = AccrueInterest(card, month);
                var balanceWithInterest = card.Balance;
                var payment = Math.Min(allocations[card.Input.CreditCardId], balanceWithInterest);
                var minimumAllocated = minsByCard[card.Input.CreditCardId];
                var minimumApplied = CreditCardMath.RoundMoney(Math.Min(payment, minimumAllocated));
                var extraApplied = CreditCardMath.RoundMoney(Math.Max(0m, payment - minimumApplied));
                var principal = CreditCardMath.RoundMoney(Math.Max(0m, payment - interest));
                ApplyPaymentToBuckets(card, payment, month);
                if (card.Balance < 0.005m)
                {
                    card.ClearBalances();
                }

                card.TotalInterest = CreditCardMath.RoundMoney(card.TotalInterest + interest);
                totalInterest = CreditCardMath.RoundMoney(totalInterest + interest);
                totalPrincipal = CreditCardMath.RoundMoney(totalPrincipal + principal);

                monthRows.Add((
                    card,
                    starting,
                    interest,
                    payment,
                    minimumApplied,
                    extraApplied,
                    principal));
            }

            if (request.EnableCashAdvanceBalanceMoves
                && request.Strategy is PayoffStrategyType.Avalanche or PayoffStrategyType.Snowball)
            {
                ApplyCashAdvanceRateTransfers(
                    monthRows.Where(r => r.ExtraApplied > 0).Select(r => r.Card).ToList(),
                    cards,
                    month,
                    // Only hold the util floor while still in the utilization phase.
                    utilizationFloorPercent: utilizationPhase ? targetUtil : null,
                    transferLegsByCard,
                    transferInByCard,
                    transferOutByCard);
            }

            var rowByCardId = monthRows.ToDictionary(r => r.Card.Input.CreditCardId);
            var emitIds = new HashSet<int>(rowByCardId.Keys);
            foreach (var card in cards)
            {
                var id = card.Input.CreditCardId;
                if (transferInByCard[id] > 0 || transferOutByCard[id] > 0)
                {
                    emitIds.Add(id);
                }
            }

            foreach (var card in cards.Where(c => emitIds.Contains(c.Input.CreditCardId)))
            {
                var cardId = card.Input.CreditCardId;
                decimal starting;
                decimal interest;
                decimal payment;
                decimal minimumApplied;
                decimal extraApplied;
                decimal principal;
                if (rowByCardId.TryGetValue(cardId, out var row))
                {
                    starting = row.Starting;
                    interest = row.Interest;
                    payment = row.Payment;
                    minimumApplied = row.MinimumApplied;
                    extraApplied = row.ExtraApplied;
                    principal = row.Principal;
                }
                else
                {
                    starting = 0;
                    interest = 0;
                    payment = 0;
                    minimumApplied = 0;
                    extraApplied = 0;
                    principal = 0;
                }

                var ending = card.Balance;
                if (ending < 0.005m)
                {
                    ending = 0;
                    card.ClearBalances();
                }

                if (ending == 0)
                {
                    if (card.PayoffDate is null)
                    {
                        card.PayoffDate = month.AddMonths(1);
                        card.MonthsToPayoff = months;
                    }
                }
                else
                {
                    card.PayoffDate = null;
                    card.MonthsToPayoff = 0;
                }

                schedule.Add(new MonthlyPayoffScheduleItem(
                    Month: month,
                    CreditCardId: cardId,
                    CreditCardName: card.Input.Name,
                    StartingBalance: starting,
                    InterestCharged: interest,
                    PaymentApplied: payment,
                    MinimumPaymentApplied: minimumApplied,
                    ExtraPaymentApplied: extraApplied,
                    PrincipalApplied: principal,
                    BalanceTransferredIn: transferInByCard[cardId],
                    BalanceTransferredOut: transferOutByCard[cardId],
                    Transfers: transferLegsByCard[cardId],
                    EndingBalance: ending));
            }

            AdvanceLoanPaymentPhase(cards);
            month = month.AddMonths(1);
        }

        if (cards.Any(c => !c.IsPaidOff))
        {
            warnings.Add("Payoff was not reached within the maximum forecast horizon (1,200 months).");
        }

        var cardOrder = cards
            .OrderBy(c => prioritySnapshot.GetValueOrDefault(c.Input.CreditCardId, int.MaxValue))
            .Select(c => new CardPayoffSummary(
                CreditCardId: c.Input.CreditCardId,
                Name: c.Input.Name,
                PriorityOrder: prioritySnapshot.GetValueOrDefault(c.Input.CreditCardId, 0),
                EstimatedPayoffDate: c.PayoffDate,
                TotalInterestPaid: c.TotalInterest,
                MonthsToPayoff: c.MonthsToPayoff))
            .ToList();

        var allPaid = cards.All(c => c.IsPaidOff);
        return new PayoffPlanResult(
            Strategy: request.Strategy,
            StartingDebt: startingDebt,
            TotalMonthlyDebtPayment: request.TotalMonthlyDebtPayment,
            CombinedMinimumPayments: CreditCardMath.RoundMoney(initialMins),
            OverallDebtFreeDate: allPaid ? cards.Max(c => c.PayoffDate) : null,
            MonthsToPayoff: months,
            TotalInterestPaid: totalInterest,
            TotalPrincipalPaid: totalPrincipal,
            InterestSavedVersusMinimums: null,
            CardOrder: cardOrder,
            Schedule: schedule,
            Warnings: warnings,
            IsValid: allPaid || request.Strategy == PayoffStrategyType.MinimumsOnly);
    }

    private static void ApplyOptionalLoan(
        PayoffPlanRequest request,
        List<CardState> cards,
        List<string> warnings)
    {
        var loanAmount = request.LoanAmount is > 0
            ? CreditCardMath.RoundMoney(request.LoanAmount.Value)
            : 0m;
        if (loanAmount <= 0)
        {
            return;
        }

        var loanApr = request.LoanAnnualPercentageRate is >= 0
            ? request.LoanAnnualPercentageRate.Value
            : 0m;
        var loanType = request.LoanType ?? LoanType.Personal;
        var schedule = LoanRepaymentScheduleBuilder.Build(new LoanScheduleRequest(
            loanType,
            loanAmount,
            loanApr,
            request.LoanTermMonths,
            request.LoanInterestOnlyMonths,
            request.LoanFixedMonthlyPayment));

        if (!schedule.IsValid)
        {
            warnings.AddRange(schedule.Errors);
            warnings.Add("Optional loan was skipped because its repayment inputs were invalid.");
            return;
        }

        var eligible = cards.Where(c => !c.IsPaidOff && !c.IsLoan).ToList();
        List<CardState> ordered;
        string applyLabel;
        var selectedIds = (request.LoanApplyCreditCardIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (selectedIds.Count > 0)
        {
            var byId = eligible.ToDictionary(c => c.Input.CreditCardId);
            ordered = [];
            foreach (var id in selectedIds)
            {
                if (byId.TryGetValue(id, out var card))
                {
                    ordered.Add(card);
                }
            }

            if (ordered.Count == 0)
            {
                warnings.Add(
                    "No selected accounts matched active credit card balances; loan proceeds were not applied to cards.");
            }

            applyLabel = "selected accounts";
        }
        else
        {
            var applyStrategy = request.LoanApplyStrategy is PayoffStrategyType.Avalanche or PayoffStrategyType.Snowball
                ? request.LoanApplyStrategy.Value
                : request.Strategy is PayoffStrategyType.Avalanche or PayoffStrategyType.Snowball
                    ? request.Strategy
                    : PayoffStrategyType.Avalanche;
            ordered = OrderByStrategy(eligible, applyStrategy, request.StartDate);
            applyLabel = applyStrategy == PayoffStrategyType.Snowball ? "Snowball" : "Avalanche";
        }

        var remaining = loanAmount;
        var appliedToCards = 0m;
        foreach (var card in ordered)
        {
            if (remaining <= 0)
            {
                break;
            }

            var take = CreditCardMath.RoundMoney(Math.Min(card.Balance, remaining));
            if (take <= 0)
            {
                continue;
            }

            ReduceBalance(card, take, preferPurchaseFirst: true);
            remaining = CreditCardMath.RoundMoney(remaining - take);
            appliedToCards = CreditCardMath.RoundMoney(appliedToCards + take);
        }

        var ioMonths = loanType == LoanType.Heloc
            ? Math.Max(0, request.LoanInterestOnlyMonths ?? 0)
            : 0;

        cards.Add(new CardState
        {
            Input = new CreditCardPayoffInput(
                CreditCardId: LoanCreditCardId,
                Name: schedule.LoanTypeDisplayName,
                CurrentBalance: loanAmount,
                CreditLimit: loanAmount,
                AnnualPercentageRate: loanApr,
                FixedMonthlyPayment: schedule.MonthlyPayment,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null),
            PurchaseBalance = loanAmount,
            CashAdvanceBalance = 0,
            LoanInterestOnlyMonthsRemaining = ioMonths,
            LoanPhase2MonthlyPayment = schedule.Phase2MonthlyPayment
        });

        warnings.Add(
            appliedToCards > 0
                ? $"A {schedule.LoanTypeDisplayName} of {loanAmount:C} at {loanApr:0.##}% APR was applied first ({applyLabel}) toward card balances ({appliedToCards:C}); the loan is included in the payoff plan."
                : $"A {schedule.LoanTypeDisplayName} of {loanAmount:C} at {loanApr:0.##}% APR is included in the payoff plan (no card balances were available to apply proceeds).");

        if (remaining > 0.005m)
        {
            warnings.Add(
                $"Loan proceeds exceeded selected/target card balances by {remaining:C}; that leftover is not modeled as cash on hand.");
        }
    }

    private static decimal AvailableCredit(CardState card)
    {
        var limit = card.Input.CreditLimit;
        if (limit <= 0)
        {
            return 0m;
        }

        return CreditCardMath.RoundMoney(Math.Max(0m, limit - card.Balance));
    }

    /// <summary>
    /// For each focus card being paid with Extra: use another card's available credit to
    /// pay it down when that card's cash-advance APR is lower. If none qualify, fall back
    /// to the highest purchase-APR card with available credit.
    /// While still in the utilization phase, never move a focus card below that floor.
    /// </summary>
    private static void ApplyCashAdvanceRateTransfers(
        IReadOnlyList<CardState> focusCards,
        List<CardState> cards,
        DateOnly month,
        decimal? utilizationFloorPercent,
        Dictionary<int, List<BalanceTransferLeg>> transferLegsByCard,
        Dictionary<int, decimal> transferInByCard,
        Dictionary<int, decimal> transferOutByCard)
    {
        foreach (var focus in focusCards)
        {
            if (focus.IsLoan || focus.Balance <= 0)
            {
                continue;
            }

            var floor = utilizationFloorPercent is not null
                ? TargetBalance(focus, utilizationFloorPercent.Value)
                : 0m;
            var movable = CreditCardMath.RoundMoney(Math.Max(0m, focus.Balance - floor));
            if (movable <= 0)
            {
                continue;
            }

            var focusApr = GetPurchaseApr(focus, month);
            var lowerCaCandidates = cards
                .Where(c =>
                    !c.IsLoan
                    && c.Input.CreditCardId != focus.Input.CreditCardId
                    && HasCashAdvanceDetail(c)
                    && TryGetCashAdvanceTerms(c, out var terms)
                    && terms.Apr < focusApr
                    && AvailableCredit(c) > 0)
                .OrderBy(c => TryGetCashAdvanceTerms(c, out var t) ? t.Apr : decimal.MaxValue)
                .ThenByDescending(AvailableCredit)
                .ThenBy(c => c.Input.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            CardState? funder;
            bool useCashAdvanceBucket;
            decimal feePercent;
            if (lowerCaCandidates.Count > 0)
            {
                funder = lowerCaCandidates[0];
                TryGetCashAdvanceTerms(funder, out var terms);
                useCashAdvanceBucket = true;
                feePercent = terms.FeePercent;
            }
            else
            {
                funder = cards
                    .Where(c =>
                        !c.IsLoan
                        && c.Input.CreditCardId != focus.Input.CreditCardId
                        && AvailableCredit(c) > 0)
                    .OrderByDescending(c => GetPurchaseApr(c, month))
                    .ThenByDescending(AvailableCredit)
                    .ThenBy(c => c.Input.Name, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                useCashAdvanceBucket = false;
                feePercent = 0m;
            }

            if (funder is null)
            {
                continue;
            }

            ExecuteBalanceMove(
                from: focus,
                to: funder,
                requestedPrincipal: movable,
                feePercent: feePercent,
                creditToCashAdvance: useCashAdvanceBucket,
                creditToPromo: false,
                promoApr: null,
                promoExpiresOn: null,
                transferLegsByCard,
                transferInByCard,
                transferOutByCard);
        }
    }

    private static void ApplyPromotionalTransfers(
        IReadOnlyList<PromotionalBalanceTransferPlan> plans,
        List<CardState> cards,
        DateOnly month,
        Dictionary<int, List<BalanceTransferLeg>> transferLegsByCard,
        Dictionary<int, decimal> transferInByCard,
        Dictionary<int, decimal> transferOutByCard)
    {
        var byId = cards.ToDictionary(c => c.Input.CreditCardId);
        foreach (var plan in plans)
        {
            if (!byId.TryGetValue(plan.FromCreditCardId, out var from)
                || !byId.TryGetValue(plan.ToCreditCardId, out var to))
            {
                continue;
            }

            if (from.Balance <= 0 || AvailableCredit(to) <= 0)
            {
                continue;
            }

            var requested = plan.Amount is > 0
                ? plan.Amount.Value
                : from.Balance;
            var expires = month.AddMonths(Math.Max(1, plan.PromotionalPeriodMonths));
            ExecuteBalanceMove(
                from,
                to,
                requestedPrincipal: requested,
                feePercent: 0m,
                creditToCashAdvance: false,
                creditToPromo: true,
                promoApr: plan.PromotionalAnnualPercentageRate,
                promoExpiresOn: expires,
                transferLegsByCard,
                transferInByCard,
                transferOutByCard);
        }
    }

    private static void ExecuteBalanceMove(
        CardState from,
        CardState to,
        decimal requestedPrincipal,
        decimal feePercent,
        bool creditToCashAdvance,
        bool creditToPromo,
        decimal? promoApr,
        DateOnly? promoExpiresOn,
        Dictionary<int, List<BalanceTransferLeg>> transferLegsByCard,
        Dictionary<int, decimal> transferInByCard,
        Dictionary<int, decimal> transferOutByCard)
    {
        var available = AvailableCredit(to);
        if (available <= 0 || requestedPrincipal <= 0 || from.Balance <= 0)
        {
            return;
        }

        var feeRate = Math.Max(0m, feePercent) / 100m;
        var maxPrincipal = feeRate > 0
            ? CreditCardMath.RoundMoney(available / (1m + feeRate))
            : available;
        var principal = CreditCardMath.RoundMoney(
            Math.Min(from.Balance, Math.Min(requestedPrincipal, maxPrincipal)));
        if (principal <= 0)
        {
            return;
        }

        var fee = CreditCardMath.RoundMoney(principal * feeRate);
        var charged = CreditCardMath.RoundMoney(principal + fee);
        if (charged > available)
        {
            charged = available;
            principal = feeRate > 0
                ? CreditCardMath.RoundMoney(charged / (1m + feeRate))
                : charged;
            fee = CreditCardMath.RoundMoney(charged - principal);
        }

        if (principal <= 0)
        {
            return;
        }

        ReduceBalance(from, principal, preferPurchaseFirst: true);
        if (creditToPromo)
        {
            to.PromoBalance = CreditCardMath.RoundMoney(to.PromoBalance + principal + fee);
            to.ActivePromoApr = promoApr;
            to.PromoExpiresOn = promoExpiresOn;
        }
        else if (creditToCashAdvance)
        {
            to.CashAdvanceBalance =
                CreditCardMath.RoundMoney(to.CashAdvanceBalance + principal + fee);
        }
        else
        {
            to.PurchaseBalance =
                CreditCardMath.RoundMoney(to.PurchaseBalance + principal + fee);
        }

        transferOutByCard[from.Input.CreditCardId] =
            CreditCardMath.RoundMoney(transferOutByCard[from.Input.CreditCardId] + principal);
        transferInByCard[to.Input.CreditCardId] =
            CreditCardMath.RoundMoney(transferInByCard[to.Input.CreditCardId] + principal + fee);

        transferLegsByCard[from.Input.CreditCardId].Add(new BalanceTransferLeg(
            CounterpartyCreditCardId: to.Input.CreditCardId,
            CounterpartyName: to.Input.Name,
            Amount: principal,
            Direction: "Out"));
        transferLegsByCard[to.Input.CreditCardId].Add(new BalanceTransferLeg(
            CounterpartyCreditCardId: from.Input.CreditCardId,
            CounterpartyName: from.Input.Name,
            Amount: CreditCardMath.RoundMoney(principal + fee),
            Direction: "In"));
    }

    private static decimal GetPurchaseApr(CardState card, DateOnly month) =>
        CreditCardMath.EffectiveApr(
            card.Input.AnnualPercentageRate,
            card.Input.PromotionalAnnualPercentageRate,
            card.Input.PromotionalRateExpirationDate,
            month);

    /// <summary>
    /// Only cards with cash-advance APR and/or fee percentage stored in credit-card detail
    /// are eligible to receive cash-advance balance moves.
    /// </summary>
    private static bool HasCashAdvanceDetail(CardState card) =>
        card.Input.CashAdvanceInterestRate is not null
        || card.Input.CashAdvanceFeePercentage is not null;

    private readonly record struct CashAdvanceTerms(decimal Apr, decimal FeePercent);

    /// <summary>
    /// Resolves cash-advance APR/fee from detail. Prefer explicit cash-advance APR; if only
    /// a percentage was entered in the fee field, treat that as the APR with no separate fee.
    /// </summary>
    private static bool TryGetCashAdvanceTerms(CardState card, out CashAdvanceTerms terms)
    {
        if (card.Input.CashAdvanceInterestRate is not null)
        {
            terms = new CashAdvanceTerms(
                card.Input.CashAdvanceInterestRate.Value,
                Math.Max(0m, card.Input.CashAdvanceFeePercentage ?? 0m));
            return true;
        }

        if (card.Input.CashAdvanceFeePercentage is not null)
        {
            terms = new CashAdvanceTerms(card.Input.CashAdvanceFeePercentage.Value, 0m);
            return true;
        }

        terms = default;
        return false;
    }

    private static decimal AccrueInterest(CardState card, DateOnly month)
    {
        ExpirePromoIfNeeded(card, month);
        var purchaseApr = GetPurchaseApr(card, month);
        var interestPurchase = CreditCardMath.RoundMoney(
            card.PurchaseBalance * CreditCardMath.MonthlyRate(purchaseApr));
        var caApr = TryGetCashAdvanceTerms(card, out var terms) ? terms.Apr : (decimal?)null;
        var interestCa = card.CashAdvanceBalance > 0 && caApr is not null
            ? CreditCardMath.RoundMoney(card.CashAdvanceBalance * CreditCardMath.MonthlyRate(caApr.Value))
            : CreditCardMath.RoundMoney(
                card.CashAdvanceBalance * CreditCardMath.MonthlyRate(purchaseApr));
        var promoApr = card.ActivePromoApr ?? purchaseApr;
        var interestPromo = CreditCardMath.RoundMoney(
            card.PromoBalance * CreditCardMath.MonthlyRate(promoApr));

        card.PurchaseBalance = CreditCardMath.RoundMoney(card.PurchaseBalance + interestPurchase);
        card.CashAdvanceBalance = CreditCardMath.RoundMoney(card.CashAdvanceBalance + interestCa);
        card.PromoBalance = CreditCardMath.RoundMoney(card.PromoBalance + interestPromo);
        return CreditCardMath.RoundMoney(interestPurchase + interestCa + interestPromo);
    }

    private static void ExpirePromoIfNeeded(CardState card, DateOnly month)
    {
        if (card.PromoBalance <= 0)
        {
            return;
        }

        if (card.PromoExpiresOn is null || month <= card.PromoExpiresOn.Value)
        {
            return;
        }

        card.PurchaseBalance = CreditCardMath.RoundMoney(card.PurchaseBalance + card.PromoBalance);
        card.PromoBalance = 0;
        card.ActivePromoApr = null;
        card.PromoExpiresOn = null;
    }

    private static void ApplyPaymentToBuckets(CardState card, decimal payment, DateOnly month)
    {
        ExpirePromoIfNeeded(card, month);
        var remaining = payment;
        var purchaseApr = GetPurchaseApr(card, month);
        var caApr = TryGetCashAdvanceTerms(card, out var terms) ? terms.Apr : purchaseApr;
        var promoApr = card.ActivePromoApr ?? purchaseApr;

        while (remaining > 0.0001m && card.Balance > 0)
        {
            // Pay the highest-rate bucket first.
            var caRate = card.CashAdvanceBalance > 0 ? caApr : -1m;
            var promoRate = card.PromoBalance > 0 ? promoApr : -1m;
            var purchaseRate = card.PurchaseBalance > 0 ? purchaseApr : -1m;

            if (caRate >= promoRate && caRate >= purchaseRate && card.CashAdvanceBalance > 0)
            {
                var apply = Math.Min(remaining, card.CashAdvanceBalance);
                card.CashAdvanceBalance = CreditCardMath.RoundMoney(card.CashAdvanceBalance - apply);
                remaining = CreditCardMath.RoundMoney(remaining - apply);
            }
            else if (promoRate >= purchaseRate && card.PromoBalance > 0)
            {
                var apply = Math.Min(remaining, card.PromoBalance);
                card.PromoBalance = CreditCardMath.RoundMoney(card.PromoBalance - apply);
                remaining = CreditCardMath.RoundMoney(remaining - apply);
            }
            else if (card.PurchaseBalance > 0)
            {
                var apply = Math.Min(remaining, card.PurchaseBalance);
                card.PurchaseBalance = CreditCardMath.RoundMoney(card.PurchaseBalance - apply);
                remaining = CreditCardMath.RoundMoney(remaining - apply);
            }
            else
            {
                break;
            }
        }
    }

    private static void ReduceBalance(CardState card, decimal amount, bool preferPurchaseFirst)
    {
        var remaining = amount;
        if (preferPurchaseFirst)
        {
            var fromPurchase = Math.Min(remaining, card.PurchaseBalance);
            card.PurchaseBalance = CreditCardMath.RoundMoney(card.PurchaseBalance - fromPurchase);
            remaining = CreditCardMath.RoundMoney(remaining - fromPurchase);
            if (remaining > 0)
            {
                var fromPromo = Math.Min(remaining, card.PromoBalance);
                card.PromoBalance = CreditCardMath.RoundMoney(card.PromoBalance - fromPromo);
                remaining = CreditCardMath.RoundMoney(remaining - fromPromo);
            }
            if (remaining > 0)
            {
                var fromCa = Math.Min(remaining, card.CashAdvanceBalance);
                card.CashAdvanceBalance = CreditCardMath.RoundMoney(card.CashAdvanceBalance - fromCa);
            }

            return;
        }

        var fromCaFirst = Math.Min(remaining, card.CashAdvanceBalance);
        card.CashAdvanceBalance = CreditCardMath.RoundMoney(card.CashAdvanceBalance - fromCaFirst);
        remaining = CreditCardMath.RoundMoney(remaining - fromCaFirst);
        if (remaining > 0)
        {
            var fromPromo = Math.Min(remaining, card.PromoBalance);
            card.PromoBalance = CreditCardMath.RoundMoney(card.PromoBalance - fromPromo);
            remaining = CreditCardMath.RoundMoney(remaining - fromPromo);
        }
        if (remaining > 0)
        {
            var fromPurchase = Math.Min(remaining, card.PurchaseBalance);
            card.PurchaseBalance = CreditCardMath.RoundMoney(card.PurchaseBalance - fromPurchase);
        }
    }

    private static PayoffStrategyType ResolveEffectiveStrategy(
        PayoffStrategyType strategy,
        PayoffStrategyType? postUtilizationStrategy,
        decimal? targetUtilizationPercent,
        bool utilizationPhase)
    {
        if (strategy is not (PayoffStrategyType.Avalanche or PayoffStrategyType.Snowball))
        {
            return strategy;
        }

        if (targetUtilizationPercent is null || utilizationPhase)
        {
            return strategy;
        }

        return postUtilizationStrategy is PayoffStrategyType.Avalanche or PayoffStrategyType.Snowball
            ? postUtilizationStrategy.Value
            : strategy;
    }

    private static decimal? NormalizeTargetUtilization(decimal? value)
    {
        if (value is null || value <= 0)
        {
            return null;
        }

        return Math.Min(99m, value.Value);
    }

    private static decimal ProjectBalanceAfterInterest(CardState card, DateOnly month)
    {
        var purchaseApr = GetPurchaseApr(card, month);
        var interestPurchase = CreditCardMath.RoundMoney(
            card.PurchaseBalance * CreditCardMath.MonthlyRate(purchaseApr));
        var caApr = TryGetCashAdvanceTerms(card, out var terms) ? terms.Apr : (decimal?)null;
        var interestCa = card.CashAdvanceBalance > 0 && caApr is not null
            ? CreditCardMath.RoundMoney(card.CashAdvanceBalance * CreditCardMath.MonthlyRate(caApr.Value))
            : CreditCardMath.RoundMoney(
                card.CashAdvanceBalance * CreditCardMath.MonthlyRate(purchaseApr));
        var promoApr = card.ActivePromoApr ?? purchaseApr;
        var interestPromo = CreditCardMath.RoundMoney(
            card.PromoBalance * CreditCardMath.MonthlyRate(promoApr));
        return CreditCardMath.RoundMoney(card.Balance + interestPurchase + interestCa + interestPromo);
    }

    private static decimal ResolveMin(CardState card)
    {
        if (card.IsLoan && card.LoanInterestOnlyMonthsRemaining > 0)
        {
            var interestOnly = CreditCardMath.RoundMoney(
                card.Balance * CreditCardMath.MonthlyRate(card.Input.AnnualPercentageRate));
            return CreditCardMath.RoundMoney(Math.Min(card.Balance + interestOnly, Math.Max(interestOnly, 0m)));
        }

        var fixedPayment = card.IsLoan && card.LoanPhase2MonthlyPayment is > 0
            && card.LoanInterestOnlyMonthsRemaining <= 0
            ? card.LoanPhase2MonthlyPayment
            : card.Input.FixedMonthlyPayment;

        return CreditCardMath.ResolveMinimumPayment(
            card.Balance,
            fixedPayment,
            card.Input.MinimumPaymentPercentage,
            card.Input.MinimumPaymentFloor);
    }

    private static void AdvanceLoanPaymentPhase(IEnumerable<CardState> cards)
    {
        foreach (var card in cards.Where(c => c.IsLoan && c.LoanInterestOnlyMonthsRemaining > 0))
        {
            card.LoanInterestOnlyMonthsRemaining--;
        }
    }

    private static bool IsOverLimit(CardState card)
    {
        if (card.IsLoan)
        {
            return false;
        }

        var limit = card.Input.CreditLimit;
        return limit > 0 && card.Balance > limit;
    }

    private static decimal OverLimitAmount(CardState card) =>
        IsOverLimit(card)
            ? CreditCardMath.RoundMoney(card.Balance - card.Input.CreditLimit)
            : 0m;

    private static bool IsAboveUtilizationTarget(CardState card, decimal targetUtilizationPercent)
    {
        if (card.IsLoan || card.Balance <= 0)
        {
            return false;
        }

        var limit = card.Input.CreditLimit;
        if (limit <= 0)
        {
            return true;
        }

        var utilization = card.Balance / limit * 100m;
        return utilization > targetUtilizationPercent;
    }

    private static decimal TargetBalance(CardState card, decimal targetUtilizationPercent)
    {
        var limit = Math.Max(0m, card.Input.CreditLimit);
        return CreditCardMath.RoundMoney(limit * (targetUtilizationPercent / 100m));
    }

    private static decimal MaxPaymentForTarget(
        CardState card,
        DateOnly month,
        decimal? targetUtilizationPercent,
        bool overLimitPhase,
        bool utilizationPhase)
    {
        var projected = ProjectBalanceAfterInterest(card, month);

        if (overLimitPhase)
        {
            if (!IsOverLimit(card) || card.Input.CreditLimit <= 0)
            {
                return 0m;
            }

            return CreditCardMath.RoundMoney(Math.Max(0m, projected - card.Input.CreditLimit));
        }

        // Utilization target: pay only enough to reach the target, then stop.
        if (targetUtilizationPercent is not null && utilizationPhase)
        {
            if (!IsAboveUtilizationTarget(card, targetUtilizationPercent.Value))
            {
                return 0m;
            }

            var targetBalance = TargetBalance(card, targetUtilizationPercent.Value);
            return CreditCardMath.RoundMoney(Math.Max(0m, projected - targetBalance));
        }

        return projected;
    }

    private static List<CardState> OrderTargets(
        IEnumerable<CardState> cards,
        PayoffStrategyType strategy,
        DateOnly asOf,
        decimal? targetUtilizationPercent,
        bool payOverLimitFirst)
    {
        var ordered = OrderByStrategy(cards, strategy, asOf);
        if (strategy is not (PayoffStrategyType.Avalanche or PayoffStrategyType.Snowball))
        {
            return ordered;
        }

        if (payOverLimitFirst)
        {
            var overLimit = ordered
                .Where(IsOverLimit)
                .OrderByDescending(OverLimitAmount)
                .ThenBy(c => ordered.IndexOf(c))
                .ToList();
            var notOverLimit = ordered.Where(c => !IsOverLimit(c)).ToList();
            ordered = overLimit.Concat(notOverLimit).ToList();
        }

        if (targetUtilizationPercent is null)
        {
            return ordered;
        }

        var above = ordered
            .Where(c => IsAboveUtilizationTarget(c, targetUtilizationPercent.Value))
            .ToList();
        var atOrBelow = ordered
            .Where(c => !IsAboveUtilizationTarget(c, targetUtilizationPercent.Value))
            .ToList();
        return above.Concat(atOrBelow).ToList();
    }

    private static List<CardState> OrderByStrategy(
        IEnumerable<CardState> cards,
        PayoffStrategyType strategy,
        DateOnly asOf) =>
        strategy switch
        {
            PayoffStrategyType.Snowball => cards
                .OrderBy(c => c.Balance)
                .ThenByDescending(c => c.Input.AnnualPercentageRate)
                .ThenBy(c => c.Input.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            PayoffStrategyType.MinimumsOnly => cards
                .OrderBy(c => c.Input.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            _ => cards
                .OrderByDescending(c => CreditCardMath.EffectiveApr(
                    c.Input.AnnualPercentageRate,
                    c.Input.PromotionalAnnualPercentageRate,
                    c.Input.PromotionalRateExpirationDate,
                    asOf))
                .ThenBy(c => c.Balance)
                .ThenBy(c => c.Input.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
}
