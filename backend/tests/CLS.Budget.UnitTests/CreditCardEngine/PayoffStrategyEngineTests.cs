using CLS.Budget.Domain.CreditCardEngine.Payoff;
using FluentAssertions;

namespace CLS.Budget.UnitTests.CreditCardEngine;

public sealed class PayoffStrategyEngineTests
{
    private readonly PayoffStrategyEngine _sut = new();

    [Fact]
    public void Avalanche_PrioritizesHighestApr()
    {
        var cards = TwoCards();
        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 400m,
            Strategy: PayoffStrategyType.Avalanche,
            StartDate: new DateOnly(2026, 1, 1)));

        result.IsValid.Should().BeTrue();
        result.CardOrder.First().Name.Should().Be("High APR");
        result.TotalInterestPaid.Should().BeGreaterThan(0);
        result.OverallDebtFreeDate.Should().NotBeNull();
    }

    [Fact]
    public void Snowball_PrioritizesLowestBalance()
    {
        var cards = TwoCards();
        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 400m,
            Strategy: PayoffStrategyType.Snowball,
            StartDate: new DateOnly(2026, 1, 1)));

        result.IsValid.Should().BeTrue();
        result.CardOrder.First().Name.Should().Be("Small Balance");
    }

    [Fact]
    public void Rejects_WhenBudgetBelowMinimums()
    {
        var cards = TwoCards();
        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 50m,
            Strategy: PayoffStrategyType.Avalanche,
            StartDate: new DateOnly(2026, 1, 1)));

        result.IsValid.Should().BeFalse();
        result.Warnings.Should().NotBeEmpty();
    }

    [Fact]
    public void PaidOffCard_FreesMinimumIntoExtraForRemainingCards()
    {
        // Fixed monthly budget stays the same; when the small card pays off,
        // its minimum rolls into Extra for the remaining card.
        var cards = new List<CreditCardPayoffInput>
        {
            new(
                CreditCardId: 1,
                Name: "Small",
                CurrentBalance: 200m,
                CreditLimit: 1000m,
                AnnualPercentageRate: 22m,
                FixedMonthlyPayment: 100m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null),
            new(
                CreditCardId: 2,
                Name: "Large",
                CurrentBalance: 2000m,
                CreditLimit: 3000m,
                AnnualPercentageRate: 18m,
                FixedMonthlyPayment: 100m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null)
        };

        const decimal monthlyBudget = 300m;
        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: monthlyBudget,
            Strategy: PayoffStrategyType.Snowball,
            StartDate: new DateOnly(2026, 1, 1)));

        result.IsValid.Should().BeTrue();

        var smallPayoffMonth = result.Schedule
            .Where(s => s.CreditCardName == "Small" && s.EndingBalance == 0m)
            .Select(s => s.Month)
            .DefaultIfEmpty()
            .Min();
        smallPayoffMonth.Should().NotBe(default(DateOnly));

        var firstMonthLargeExtra = result.Schedule
            .Where(s => s.CreditCardName == "Large" && s.Month == new DateOnly(2026, 1, 1))
            .Select(s => s.ExtraPaymentApplied)
            .Single();

        var afterSmallPaidLargeExtra = result.Schedule
            .Where(s =>
                s.CreditCardName == "Large"
                && s.Month > smallPayoffMonth
                && s.PaymentApplied > 0)
            .Select(s => s.ExtraPaymentApplied)
            .DefaultIfEmpty(0m)
            .Max();

        // Before Small is gone: mins $200, Extra pool ~$100.
        // After Small is gone: mins $100, Extra pool ~$200 for Large.
        afterSmallPaidLargeExtra.Should().BeGreaterThan(firstMonthLargeExtra);
        afterSmallPaidLargeExtra.Should().BeApproximately(200m, 0.05m);
    }

    [Fact]
    public void Avalanche_SavesInterestVersusSnowball_ForTypicalInputs()
    {
        var cards = TwoCards();
        var avalanche = _sut.GeneratePlan(new PayoffPlanRequest(
            cards, 400m, PayoffStrategyType.Avalanche, new DateOnly(2026, 1, 1)));
        var snowball = _sut.GeneratePlan(new PayoffPlanRequest(
            cards, 400m, PayoffStrategyType.Snowball, new DateOnly(2026, 1, 1)));

        avalanche.TotalInterestPaid.Should().BeLessThanOrEqualTo(snowball.TotalInterestPaid);
    }

    [Fact]
    public void Snowball_WithUtilizationTarget_AttacksLowestBalanceAboveTargetFirst()
    {
        var cards = new List<CreditCardPayoffInput>
        {
            new(
                CreditCardId: 1,
                Name: "Large",
                CurrentBalance: 4000m,
                CreditLimit: 5000m,
                AnnualPercentageRate: 20m,
                FixedMonthlyPayment: 80m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null),
            new(
                CreditCardId: 2,
                Name: "Small",
                CurrentBalance: 900m,
                CreditLimit: 1000m,
                AnnualPercentageRate: 15m,
                FixedMonthlyPayment: 40m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null)
        };

        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 500m,
            Strategy: PayoffStrategyType.Snowball,
            StartDate: new DateOnly(2026, 1, 1),
            TargetUtilizationPercent: 30m));

        result.IsValid.Should().BeTrue();
        result.CardOrder.First().Name.Should().Be("Small");

        var firstMonth = result.Schedule.Where(s => s.Month == new DateOnly(2026, 1, 1)).ToList();
        firstMonth.Single(s => s.CreditCardName == "Small").PaymentApplied
            .Should().BeGreaterThan(
                firstMonth.Single(s => s.CreditCardName == "Large").PaymentApplied);
        firstMonth.Single(s => s.CreditCardName == "Large").PaymentApplied.Should().Be(80m);
    }

    [Fact]
    public void PostUtilizationStrategy_SwitchesOrderAfterTargetMet()
    {
        // After both cards hit 50% util, Avalanche phase-1 then Snowball phase-2
        // should attack the lower remaining balance first.
        var cards = new List<CreditCardPayoffInput>
        {
            new(
                CreditCardId: 1,
                Name: "High APR larger remaining",
                CurrentBalance: 900m,
                CreditLimit: 1000m,
                AnnualPercentageRate: 28m,
                FixedMonthlyPayment: 40m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null),
            new(
                CreditCardId: 2,
                Name: "Low APR smaller remaining",
                CurrentBalance: 800m,
                CreditLimit: 1000m,
                AnnualPercentageRate: 12m,
                FixedMonthlyPayment: 40m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null)
        };

        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 500m,
            Strategy: PayoffStrategyType.Avalanche,
            StartDate: new DateOnly(2026, 1, 1),
            TargetUtilizationPercent: 50m,
            PostUtilizationStrategy: PayoffStrategyType.Snowball));

        result.IsValid.Should().BeTrue();

        // Find first month where both cards are already at/below 50% at start
        // (phase 2). Prefer the smaller remaining balance for Extra.
        var byMonth = result.Schedule.GroupBy(s => s.Month).OrderBy(g => g.Key);
        foreach (var monthGroup in byMonth)
        {
            var rows = monthGroup.ToList();
            if (rows.Count < 2) continue;
            if (rows.All(r => r.StartingBalance <= 500.05m))
            {
                var smaller = rows.OrderBy(r => r.StartingBalance).First();
                var larger = rows.OrderByDescending(r => r.StartingBalance).First();
                if (smaller.StartingBalance < larger.StartingBalance - 1m
                    && rows.Sum(r => r.ExtraPaymentApplied) > 0)
                {
                    smaller.ExtraPaymentApplied.Should().BeGreaterThan(
                        larger.ExtraPaymentApplied);
                    return;
                }
            }
        }

        throw new Xunit.Sdk.XunitException(
            "Expected a phase-2 month where Snowball Extra preferred the smaller balance.");
    }

    [Fact]
    public void UtilizationTarget_ThenContinuesPayoffToZero()
    {
        var cards = new List<CreditCardPayoffInput>
        {
            new(
                CreditCardId: 1,
                Name: "A",
                CurrentBalance: 800m,
                CreditLimit: 1000m,
                AnnualPercentageRate: 20m,
                FixedMonthlyPayment: 25m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null),
            new(
                CreditCardId: 2,
                Name: "B",
                CurrentBalance: 700m,
                CreditLimit: 1000m,
                AnnualPercentageRate: 18m,
                FixedMonthlyPayment: 25m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null)
        };

        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 400m,
            Strategy: PayoffStrategyType.Avalanche,
            StartDate: new DateOnly(2026, 1, 1),
            TargetUtilizationPercent: 30m));

        result.IsValid.Should().BeTrue();
        result.OverallDebtFreeDate.Should().NotBeNull();

        // Phase 1: first month should not pay past the 30% floor on the focus card.
        var firstMonthA = result.Schedule
            .Where(s => s.CreditCardName == "A" && s.Month == new DateOnly(2026, 1, 1))
            .Single();
        firstMonthA.EndingBalance.Should().BeGreaterThanOrEqualTo(300m);

        // Phase 2: continue with the selected strategy until paid off.
        foreach (var cardName in new[] { "A", "B" })
        {
            result.Schedule
                .Where(s => s.CreditCardName == cardName)
                .OrderByDescending(s => s.Month)
                .First()
                .EndingBalance.Should().Be(0m);
        }
    }

    [Fact]
    public void Avalanche_PayOverLimitFirst_AttacksOverLimitCardBeforeHigherApr()
    {
        var cards = new List<CreditCardPayoffInput>
        {
            new(
                CreditCardId: 1,
                Name: "High APR under limit",
                CurrentBalance: 2000m,
                CreditLimit: 5000m,
                AnnualPercentageRate: 28m,
                FixedMonthlyPayment: 50m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null),
            new(
                CreditCardId: 2,
                Name: "Over limit",
                CurrentBalance: 1100m,
                CreditLimit: 1000m,
                AnnualPercentageRate: 12m,
                FixedMonthlyPayment: 40m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null)
        };

        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 400m,
            Strategy: PayoffStrategyType.Avalanche,
            StartDate: new DateOnly(2026, 1, 1),
            PayOverLimitFirst: true));

        result.IsValid.Should().BeTrue();
        result.CardOrder.First().Name.Should().Be("Over limit");

        var firstMonth = result.Schedule.Where(s => s.Month == new DateOnly(2026, 1, 1)).ToList();
        firstMonth.Single(s => s.CreditCardName == "Over limit").PaymentApplied
            .Should().BeGreaterThan(
                firstMonth.Single(s => s.CreditCardName == "High APR under limit").PaymentApplied);
        firstMonth.Single(s => s.CreditCardName == "High APR under limit").PaymentApplied
            .Should().Be(50m);
    }

    [Fact]
    public void Avalanche_WithUtilizationTarget_AttacksHighestAprAboveTargetFirst()
    {
        var cards = new List<CreditCardPayoffInput>
        {
            new(
                CreditCardId: 1,
                Name: "High APR",
                CurrentBalance: 2000m,
                CreditLimit: 4000m,
                AnnualPercentageRate: 28m,
                FixedMonthlyPayment: 50m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null),
            new(
                CreditCardId: 2,
                Name: "Low APR",
                CurrentBalance: 2000m,
                CreditLimit: 4000m,
                AnnualPercentageRate: 12m,
                FixedMonthlyPayment: 50m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null)
        };

        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 400m,
            Strategy: PayoffStrategyType.Avalanche,
            StartDate: new DateOnly(2026, 1, 1),
            TargetUtilizationPercent: 30m));

        result.IsValid.Should().BeTrue();
        var firstMonth = result.Schedule.Where(s => s.Month == new DateOnly(2026, 1, 1)).ToList();
        firstMonth.Single(s => s.CreditCardName == "High APR").PaymentApplied
            .Should().BeGreaterThan(
                firstMonth.Single(s => s.CreditCardName == "Low APR").PaymentApplied);
    }

    [Fact]
    public void CashAdvanceArbitrage_RespectsUtilizationFloor()
    {
        // Mirrors Credit One-style: payment leaves a remainder, then CA assist must not
        // wipe the card below the utilization target.
        var cards = new List<CreditCardPayoffInput>
        {
            new(
                CreditCardId: 1,
                Name: "Credit One",
                CurrentBalance: 500m,
                CreditLimit: 500m,
                AnnualPercentageRate: 27.48m,
                FixedMonthlyPayment: 30m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null),
            new(
                CreditCardId: 2,
                Name: "HVCU",
                CurrentBalance: 100m,
                CreditLimit: 5000m,
                AnnualPercentageRate: 18m,
                FixedMonthlyPayment: 25m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null,
                CashAdvanceInterestRate: 5m,
                CashAdvanceFeePercentage: 17m)
        };

        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 330m,
            Strategy: PayoffStrategyType.Avalanche,
            StartDate: new DateOnly(2026, 7, 1),
            TargetUtilizationPercent: 30m,
            EnableCashAdvanceBalanceMoves: true));

        result.IsValid.Should().BeTrue();
        var creditOneRows = result.Schedule
            .Where(s => s.CreditCardName == "Credit One")
            .OrderBy(s => s.Month)
            .ToList();
        creditOneRows.Should().NotBeEmpty();

        // While still above/at the util target in month 1, CA must not wipe below 30%.
        var first = creditOneRows.First();
        first.EndingBalance.Should().BeGreaterThanOrEqualTo(150m);
        first.BalanceTransferredOut.Should().BeGreaterThan(0);

        // After utilization is met, the plan continues and can pay the card off.
        creditOneRows.Last().EndingBalance.Should().Be(0m);
    }

    [Fact]
    public void CashAdvanceArbitrage_MovesBalanceWhenCashAdvanceAprIsLower()
    {
        var cards = new List<CreditCardPayoffInput>
        {
            new(
                CreditCardId: 1,
                Name: "High Purchase APR",
                CurrentBalance: 2000m,
                CreditLimit: 2500m,
                AnnualPercentageRate: 24m,
                FixedMonthlyPayment: 50m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null),
            new(
                CreditCardId: 2,
                Name: "Low Cash Advance APR",
                CurrentBalance: 200m,
                CreditLimit: 3000m,
                AnnualPercentageRate: 22m,
                FixedMonthlyPayment: 25m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null,
                CashAdvanceInterestRate: 5m,
                CashAdvanceFeePercentage: null)
        };

        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 200m,
            Strategy: PayoffStrategyType.Avalanche,
            StartDate: new DateOnly(2026, 1, 1),
            EnableCashAdvanceBalanceMoves: true));

        result.IsValid.Should().BeTrue();
        var firstMonth = result.Schedule
            .Where(s => s.Month == new DateOnly(2026, 1, 1))
            .ToList();
        var source = firstMonth.Single(s => s.CreditCardName == "High Purchase APR");
        var dest = firstMonth.Single(s => s.CreditCardName == "Low Cash Advance APR");

        source.ExtraPaymentApplied.Should().BeGreaterThan(0);
        source.BalanceTransferredOut.Should().BeGreaterThan(0);
        dest.BalanceTransferredIn.Should().Be(source.BalanceTransferredOut);
        dest.EndingBalance.Should().BeLessThanOrEqualTo(3000m);
        dest.Transfers.Should().Contain(t =>
            t.Direction == "In" && t.CounterpartyName == "High Purchase APR");
    }

    [Fact]
    public void CashAdvanceArbitrage_OnlyMovesOntoCardsWithCashAdvanceDetail()
    {
        var cards = new List<CreditCardPayoffInput>
        {
            new(
                CreditCardId: 1,
                Name: "High Purchase APR",
                CurrentBalance: 2000m,
                CreditLimit: 2500m,
                AnnualPercentageRate: 24m,
                FixedMonthlyPayment: 50m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null),
            new(
                CreditCardId: 2,
                Name: "No Cash Advance Detail",
                CurrentBalance: 200m,
                CreditLimit: 3000m,
                AnnualPercentageRate: 10m,
                FixedMonthlyPayment: 25m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null),
            new(
                CreditCardId: 3,
                Name: "Has Cash Advance Percentage",
                CurrentBalance: 100m,
                CreditLimit: 2000m,
                AnnualPercentageRate: 20m,
                FixedMonthlyPayment: 20m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null,
                CashAdvanceInterestRate: null,
                CashAdvanceFeePercentage: 5m)
        };

        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 200m,
            Strategy: PayoffStrategyType.Avalanche,
            StartDate: new DateOnly(2026, 1, 1),
            EnableCashAdvanceBalanceMoves: true));

        result.IsValid.Should().BeTrue();
        var firstMonth = result.Schedule
            .Where(s => s.Month == new DateOnly(2026, 1, 1))
            .ToList();
        var source = firstMonth.Single(s => s.CreditCardName == "High Purchase APR");
        var noDetail = firstMonth.Single(s => s.CreditCardName == "No Cash Advance Detail");
        var withDetail = firstMonth.Single(s => s.CreditCardName == "Has Cash Advance Percentage");

        source.BalanceTransferredOut.Should().BeGreaterThan(0);
        noDetail.BalanceTransferredIn.Should().Be(0);
        withDetail.BalanceTransferredIn.Should().Be(source.BalanceTransferredOut);
    }

    [Fact]
    public void PromotionalTransfer_MovesBalanceInScheduledMonth()
    {
        var cards = new List<CreditCardPayoffInput>
        {
            new(
                CreditCardId: 1,
                Name: "High APR",
                CurrentBalance: 2000m,
                CreditLimit: 2500m,
                AnnualPercentageRate: 24m,
                FixedMonthlyPayment: 50m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null),
            new(
                CreditCardId: 2,
                Name: "Promo Card",
                CurrentBalance: 100m,
                CreditLimit: 5000m,
                AnnualPercentageRate: 22m,
                FixedMonthlyPayment: 25m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null)
        };

        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 75m,
            Strategy: PayoffStrategyType.Avalanche,
            StartDate: new DateOnly(2026, 1, 1),
            PromotionalTransfers:
            [
                new PromotionalBalanceTransferPlan(
                    FromCreditCardId: 1,
                    ToCreditCardId: 2,
                    Amount: 500m,
                    PromotionalAnnualPercentageRate: 0m,
                    PromotionalPeriodMonths: 12,
                    ApplyAtMonthOffset: 0)
            ]));

        result.IsValid.Should().BeTrue();
        var firstMonth = result.Schedule
            .Where(s => s.Month == new DateOnly(2026, 1, 1))
            .ToList();
        firstMonth.Single(s => s.CreditCardName == "High APR")
            .BalanceTransferredOut.Should().Be(500m);
        firstMonth.Single(s => s.CreditCardName == "Promo Card")
            .BalanceTransferredIn.Should().Be(500m);
    }

    [Fact]
    public void CashAdvanceArbitrage_DoesNothingWhenStepDisabled()
    {
        var cards = new List<CreditCardPayoffInput>
        {
            new(
                CreditCardId: 1,
                Name: "High Purchase APR",
                CurrentBalance: 2000m,
                CreditLimit: 2500m,
                AnnualPercentageRate: 24m,
                FixedMonthlyPayment: 50m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null),
            new(
                CreditCardId: 2,
                Name: "Low Cash Advance APR",
                CurrentBalance: 200m,
                CreditLimit: 3000m,
                AnnualPercentageRate: 22m,
                FixedMonthlyPayment: 25m,
                MinimumPaymentPercentage: null,
                MinimumPaymentFloor: null,
                PromotionalAnnualPercentageRate: null,
                PromotionalRateExpirationDate: null,
                CashAdvanceInterestRate: 5m,
                CashAdvanceFeePercentage: null)
        };

        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 200m,
            Strategy: PayoffStrategyType.Avalanche,
            StartDate: new DateOnly(2026, 1, 1),
            EnableCashAdvanceBalanceMoves: false));

        result.Schedule.Should().OnlyContain(s =>
            s.BalanceTransferredIn == 0 && s.BalanceTransferredOut == 0);
    }

    [Fact]
    public void Loan_AppliesProceedsFirstAndIncludesLoanDebt()
    {
        var cards = TwoCards();
        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 400m,
            Strategy: PayoffStrategyType.Avalanche,
            StartDate: new DateOnly(2026, 1, 1),
            LoanAmount: 800m,
            LoanAnnualPercentageRate: 9m,
            LoanApplyStrategy: PayoffStrategyType.Avalanche,
            LoanType: CLS.Budget.Domain.CreditCardEngine.Loan.LoanType.Personal,
            LoanTermMonths: 24));

        result.IsValid.Should().BeTrue();
        result.CardOrder.Should().Contain(c => c.CreditCardId == PayoffStrategyEngine.LoanCreditCardId);
        result.StartingDebt.Should().Be(3800m); // 3000 + 800 cards, loan applies then re-adds loan debt
        // Avalanche applies loan to highest APR first: High APR 3000 -> 2200.
        var firstHighApr = result.Schedule
            .Where(s => s.CreditCardId == 1)
            .OrderBy(s => s.Month)
            .First();
        firstHighApr.StartingBalance.Should().Be(2200m);
        var firstLoan = result.Schedule
            .Where(s => s.CreditCardId == PayoffStrategyEngine.LoanCreditCardId)
            .OrderBy(s => s.Month)
            .First();
        firstLoan.StartingBalance.Should().Be(800m);
    }

    [Fact]
    public void Loan_SnowballApplyStrategy_TargetsLowestBalanceFirst()
    {
        var cards = TwoCards();
        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 400m,
            Strategy: PayoffStrategyType.Avalanche,
            StartDate: new DateOnly(2026, 1, 1),
            LoanAmount: 800m,
            LoanAnnualPercentageRate: 9m,
            LoanApplyStrategy: PayoffStrategyType.Snowball,
            LoanType: CLS.Budget.Domain.CreditCardEngine.Loan.LoanType.Personal,
            LoanTermMonths: 24));

        result.IsValid.Should().BeTrue();
        // Snowball applies loan to lowest balance first: Small Balance 800 -> 0 (no schedule rows).
        result.Schedule.Where(s => s.CreditCardId == 2).Should().BeEmpty();
        var firstHighApr = result.Schedule
            .Where(s => s.CreditCardId == 1)
            .OrderBy(s => s.Month)
            .First();
        firstHighApr.StartingBalance.Should().Be(3000m);
    }

    [Fact]
    public void Loan_SelectedAccounts_AppliesOnlyToChosenCards()
    {
        var cards = TwoCards();
        var result = _sut.GeneratePlan(new PayoffPlanRequest(
            CreditCards: cards,
            TotalMonthlyDebtPayment: 400m,
            Strategy: PayoffStrategyType.Avalanche,
            StartDate: new DateOnly(2026, 1, 1),
            LoanAmount: 800m,
            LoanAnnualPercentageRate: 9m,
            LoanApplyStrategy: PayoffStrategyType.Avalanche,
            LoanType: CLS.Budget.Domain.CreditCardEngine.Loan.LoanType.Personal,
            LoanTermMonths: 24,
            LoanApplyCreditCardIds: [2]));

        result.IsValid.Should().BeTrue();
        // Selected Small Balance (id 2) only: 800 -> 0; High APR stays 3000.
        result.Schedule.Where(s => s.CreditCardId == 2).Should().BeEmpty();
        var firstHighApr = result.Schedule
            .Where(s => s.CreditCardId == 1)
            .OrderBy(s => s.Month)
            .First();
        firstHighApr.StartingBalance.Should().Be(3000m);
    }

    private static List<CreditCardPayoffInput> TwoCards() =>
    [
        new CreditCardPayoffInput(
            CreditCardId: 1,
            Name: "High APR",
            CurrentBalance: 3000m,
            CreditLimit: 5000m,
            AnnualPercentageRate: 24m,
            FixedMonthlyPayment: 100m,
            MinimumPaymentPercentage: null,
            MinimumPaymentFloor: null,
            PromotionalAnnualPercentageRate: null,
            PromotionalRateExpirationDate: null),
        new CreditCardPayoffInput(
            CreditCardId: 2,
            Name: "Small Balance",
            CurrentBalance: 800m,
            CreditLimit: 2000m,
            AnnualPercentageRate: 12m,
            FixedMonthlyPayment: 50m,
            MinimumPaymentPercentage: null,
            MinimumPaymentFloor: null,
            PromotionalAnnualPercentageRate: null,
            PromotionalRateExpirationDate: null)
    ];
}
