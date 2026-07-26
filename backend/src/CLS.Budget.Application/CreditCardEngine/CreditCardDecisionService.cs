using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Common;
using CLS.Budget.Application.CreditCardEngine.Dtos;
using CLS.Budget.Application.CreditCards;
using CLS.Budget.Domain.CreditCardEngine;
using CLS.Budget.Domain.CreditCardEngine.BalanceTransfer;
using CLS.Budget.Domain.CreditCardEngine.CashFlow;
using CLS.Budget.Domain.CreditCardEngine.Forecast;
using CLS.Budget.Domain.CreditCardEngine.Interest;
using CLS.Budget.Domain.CreditCardEngine.Loan;
using CLS.Budget.Domain.CreditCardEngine.Payoff;
using CLS.Budget.Domain.CreditCardEngine.Utilization;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Application.CreditCardEngine.Validators;
using System.Text.Json;

namespace CLS.Budget.Application.CreditCardEngine;

public sealed partial class CreditCardDecisionService(
    IAccountRepository accountRepository,
    IForecastScenarioRepository forecastScenarioRepository,
    ISavedPayoffPlanRepository savedPayoffPlanRepository,
    IActivePayoffPlanRepository activePayoffPlanRepository,
    IInterestCalculationEngine interestEngine,
    IUtilizationEngine utilizationEngine,
    IPayoffStrategyEngine payoffEngine,
    IBalanceTransferEngine balanceTransferEngine,
    ICashFlowEngine cashFlowEngine,
    IForecastEngine forecastEngine) : ICreditCardDecisionService
{
    private static readonly string[] DefaultAssumptions =
    [
        "No new charges are added during the payoff period.",
        "Interest is calculated before applying each monthly payment.",
        "Extra funds roll to the next target card after a card is paid off.",
        "When a utilization target is set, Avalanche and Snowball first bring each card to that utilization using their strategy order, then finish paying balances to zero.",
        "Results are estimates for educational and planning purposes only."
    ];

    private static readonly string[] BalanceTransferAssumptions =
    [
        "Scenarios are compared over the promotional period only.",
        "Interest is calculated before applying each monthly payment.",
        "The transfer fee is added to the new balance unless the offer handles it separately.",
        "No new charges are added during the comparison period.",
        "Results are estimates for educational and planning purposes only."
    ];

    private static readonly string[] CashFlowAssumptions =
    [
        "Disposable income equals net income plus additional funds, minus required expenses, variable expenses, debt minimums, and savings.",
        "Safe extra payment leaves the selected safety buffer unspent.",
        "Aggressive extra payment uses all disposable income and leaves no buffer.",
        "Results are estimates for educational and planning purposes only."
    ];

    private static readonly string[] ForecastAssumptions =
    [
        "Monthly balances follow the same allocation rules as the payoff strategy engine.",
        "Interest is calculated before applying each monthly payment.",
        "Available cash is income minus expenses minus debt payments for that month.",
        "Results are estimates for educational and planning purposes only."
    ];

    public async Task<ApiResponse<CalculationEnvelope<ComparePayoffPlansResultDto>>> ComparePayoffPlansAsync(
        ComparePayoffPlansRequest request,
        CancellationToken cancellationToken = default)
    {
        var accounts = await LoadCreditCardsAsync(cancellationToken);
        var withBalance = accounts
            .Where(a => !a.IsPaidOff && a.Balance > 0)
            .ToList();
        var excluded = withBalance.Where(a => !IsIncludedInPayoffAnalysis(a)).ToList();
        var inputs = withBalance
            .Where(IsIncludedInPayoffAnalysis)
            .Select(ToPayoffInput)
            .ToList();

        if (inputs.Count == 0)
        {
            return ApiResponse<CalculationEnvelope<ComparePayoffPlansResultDto>>.Fail(
                excluded.Count > 0
                    ? "No credit cards remain for payoff analysis after exclusions."
                    : "No active credit card balances were found to compare.");
        }

        var startDate = ToDateOnly(request.StartDate) ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var startingDebt = CreditCardMath.RoundMoney(inputs.Sum(c => c.CurrentBalance));
        var targetUtilization = request.TargetUtilizationPercent is > 0 and <= 99
            ? request.TargetUtilizationPercent
            : null;

        var promoTransfers = (request.PromotionalTransfers ?? [])
            .Where(t => t.FromCreditCardId != t.ToCreditCardId && t.PromotionalPeriodMonths > 0)
            .Select(t => new PromotionalBalanceTransferPlan(
                t.FromCreditCardId,
                t.ToCreditCardId,
                t.Amount is > 0 ? t.Amount : null,
                t.PromotionalAnnualPercentageRate,
                t.PromotionalPeriodMonths,
                Math.Max(0, t.ApplyAtMonthOffset)))
            .ToList();

        var postUtilizationStrategy = ParsePostUtilizationStrategy(request.PostUtilizationStrategy);
        var loan = ResolveLoanPlanArgs(
            request.LoanAmount,
            request.LoanAnnualPercentageRate,
            request.LoanApplyStrategy,
            request.LoanApplyCreditCardIds,
            request.LoanType,
            request.LoanTermMonths,
            request.LoanInterestOnlyMonths,
            request.LoanFixedMonthlyPayment);
        var totalPayment = CreditCardMath.RoundMoney(
            request.TotalMonthlyDebtPayment + loan.RequiredMonthlyBudget);

        var avalanche = payoffEngine.GeneratePlan(new PayoffPlanRequest(
            inputs,
            totalPayment,
            PayoffStrategyType.Avalanche,
            startDate,
            targetUtilization,
            request.PayOverLimitFirst,
            request.EnableCashAdvanceBalanceMoves,
            promoTransfers,
            postUtilizationStrategy,
            loan.Amount,
            loan.Apr,
            loan.ApplyStrategy,
            loan.Type,
            loan.TermMonths,
            loan.InterestOnlyMonths,
            loan.FixedMonthlyPayment,
            loan.ApplyCreditCardIds));
        var snowball = payoffEngine.GeneratePlan(new PayoffPlanRequest(
            inputs,
            totalPayment,
            PayoffStrategyType.Snowball,
            startDate,
            targetUtilization,
            request.PayOverLimitFirst,
            request.EnableCashAdvanceBalanceMoves,
            promoTransfers,
            postUtilizationStrategy,
            loan.Amount,
            loan.Apr,
            loan.ApplyStrategy,
            loan.Type,
            loan.TermMonths,
            loan.InterestOnlyMonths,
            loan.FixedMonthlyPayment,
            loan.ApplyCreditCardIds));
        var minimums = payoffEngine.GeneratePlan(new PayoffPlanRequest(
            inputs,
            totalPayment,
            PayoffStrategyType.MinimumsOnly,
            startDate,
            LoanAmount: loan.Amount,
            LoanAnnualPercentageRate: loan.Apr,
            LoanApplyStrategy: loan.ApplyStrategy,
            LoanType: loan.Type,
            LoanTermMonths: loan.TermMonths,
            LoanInterestOnlyMonths: loan.InterestOnlyMonths,
            LoanFixedMonthlyPayment: loan.FixedMonthlyPayment,
            LoanApplyCreditCardIds: loan.ApplyCreditCardIds));

        var strategies = new[]
        {
            ToStrategyDto(avalanche, minimums),
            ToStrategyDto(snowball, minimums),
            ToStrategyDto(minimums, minimums)
        };

        string? recommended = null;
        string? reason = null;
        // Recommend among Avalanche/Snowball only (Minimums is a baseline, not a race winner).
        var valid = strategies
            .Where(s => s.IsValid
                && !string.Equals(s.Strategy, "MinimumsOnly", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (valid.Count > 0)
        {
            var best = valid
                .OrderBy(s => s.TotalInterest)
                .ThenBy(s => s.MonthsToPayoff)
                .First();
            recommended = best.Strategy;
            var other = valid.FirstOrDefault(s => s.Strategy != best.Strategy);
            if (other is not null)
            {
                var interestSaved = CreditCardMath.RoundMoney(other.TotalInterest - best.TotalInterest);
                var monthsSaved = other.MonthsToPayoff - best.MonthsToPayoff;
                if (interestSaved == 0m && monthsSaved == 0)
                {
                    recommended = null;
                    reason =
                        "Avalanche and Snowball produce the same payoff at this payment amount. "
                        + "Increase the monthly payment above combined minimums so extra funds can be applied by strategy.";
                }
                else
                {
                    reason =
                        $"The {best.Strategy.ToLowerInvariant()} strategy is estimated to save {interestSaved:C} in interest"
                        + (monthsSaved > 0 ? $" and finish {monthsSaved} month(s) earlier." : ".");
                }
            }
            else
            {
                reason = $"{best.Strategy} is the only valid strategy for the current payment budget.";
            }
        }

        var warnings = strategies.SelectMany(s => s.Warnings).Distinct().ToList();
        if (loan.ScheduleWarnings.Count > 0)
        {
            warnings.AddRange(loan.ScheduleWarnings);
        }
        if (excluded.Count > 0)
        {
            var names = string.Join(", ", excluded.Select(a => a.Name));
            warnings.Add(
                excluded.Count == 1
                    ? $"{names} is excluded from payoff analysis."
                    : $"{excluded.Count} cards are excluded from payoff analysis: {names}.");
        }
        if (!avalanche.IsValid && !snowball.IsValid)
        {
            warnings.Add("Increase the monthly payment to at least the combined card minimums.");
        }

        var result = new ComparePayoffPlansResultDto
        {
            StartingDebt = startingDebt,
            MonthlyPayment = request.TotalMonthlyDebtPayment,
            Strategies = strategies,
            RecommendedStrategy = recommended,
            Reason = reason
        };

        return ApiResponse<CalculationEnvelope<ComparePayoffPlansResultDto>>.Ok(
            CalculationEnvelope<ComparePayoffPlansResultDto>.Create(result, DefaultAssumptions, warnings));
    }

    public async Task<ApiResponse<CalculationEnvelope<CompareLoanSavingsResultDto>>> CompareLoanSavingsAsync(
        CompareLoanSavingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var accounts = await LoadCreditCardsAsync(cancellationToken);
        var withBalance = accounts
            .Where(a => !a.IsPaidOff && a.Balance > 0)
            .ToList();
        var excluded = withBalance.Where(a => !IsIncludedInPayoffAnalysis(a)).ToList();
        var inputs = withBalance
            .Where(IsIncludedInPayoffAnalysis)
            .Select(ToPayoffInput)
            .ToList();

        if (inputs.Count == 0)
        {
            return ApiResponse<CalculationEnvelope<CompareLoanSavingsResultDto>>.Fail(
                excluded.Count > 0
                    ? "No credit cards remain for payoff analysis after exclusions."
                    : "No active credit card balances were found to compare.");
        }

        var strategy = CreateForecastRequestValidator.ParseStrategy(request.Strategy)
            ?? PayoffStrategyType.Avalanche;
        var startDate = ToDateOnly(request.StartDate) ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var targetUtilization = request.TargetUtilizationPercent is > 0 and <= 99
            ? request.TargetUtilizationPercent
            : null;
        var promoTransfers = (request.PromotionalTransfers ?? [])
            .Where(t => t.FromCreditCardId != t.ToCreditCardId && t.PromotionalPeriodMonths > 0)
            .Select(t => new PromotionalBalanceTransferPlan(
                t.FromCreditCardId,
                t.ToCreditCardId,
                t.Amount is > 0 ? t.Amount : null,
                t.PromotionalAnnualPercentageRate,
                t.PromotionalPeriodMonths,
                Math.Max(0, t.ApplyAtMonthOffset)))
            .ToList();
        var postUtilizationStrategy = ParsePostUtilizationStrategy(request.PostUtilizationStrategy);

        var loan = ResolveLoanPlanArgs(
            request.LoanAmount,
            request.LoanAnnualPercentageRate,
            request.LoanApplyStrategy,
            request.LoanApplyCreditCardIds,
            request.LoanType,
            request.LoanTermMonths,
            request.LoanInterestOnlyMonths,
            request.LoanFixedMonthlyPayment);

        if (loan.Amount is null)
        {
            return ApiResponse<CalculationEnvelope<CompareLoanSavingsResultDto>>.Fail(
                loan.ScheduleWarnings.Count > 0
                    ? loan.ScheduleWarnings
                    : ["Enter a valid loan amount, type, and repayment details before comparing savings."]);
        }

        var withoutLoan = payoffEngine.GeneratePlan(new PayoffPlanRequest(
            inputs,
            request.TotalMonthlyDebtPayment,
            strategy,
            startDate,
            targetUtilization,
            request.PayOverLimitFirst,
            request.EnableCashAdvanceBalanceMoves,
            promoTransfers,
            postUtilizationStrategy));

        var withLoanBudget = CreditCardMath.RoundMoney(
            request.TotalMonthlyDebtPayment + loan.RequiredMonthlyBudget);
        var withLoan = payoffEngine.GeneratePlan(new PayoffPlanRequest(
            inputs,
            withLoanBudget,
            strategy,
            startDate,
            targetUtilization,
            request.PayOverLimitFirst,
            request.EnableCashAdvanceBalanceMoves,
            promoTransfers,
            postUtilizationStrategy,
            loan.Amount,
            loan.Apr,
            loan.ApplyStrategy,
            loan.Type,
            loan.TermMonths,
            loan.InterestOnlyMonths,
            loan.FixedMonthlyPayment,
            loan.ApplyCreditCardIds));

        var withoutDto = ToLoanSavingsScenario(
            "Continue monthly payments (no loan)",
            withoutLoan);
        var withDto = ToLoanSavingsScenario(
            "Take the loan",
            withLoan);

        var interestSaved = withoutDto.IsValid && withDto.IsValid
            ? CreditCardMath.RoundMoney(withoutDto.TotalInterest - withDto.TotalInterest)
            : 0m;
        var monthsSaved = withoutDto.IsValid && withDto.IsValid
            ? withoutDto.MonthsToPayoff - withDto.MonthsToPayoff
            : 0;
        var totalPaidSaved = withoutDto.IsValid && withDto.IsValid
            ? CreditCardMath.RoundMoney(withoutDto.TotalPaid - withDto.TotalPaid)
            : 0m;

        string summary;
        if (!withoutDto.IsValid || !withDto.IsValid)
        {
            summary =
                "One or both scenarios could not finish payoff at the current monthly payment. "
                + "Increase monthly payments or adjust loan terms, then try again.";
        }
        else if (interestSaved > 0)
        {
            summary =
                $"Taking this loan is estimated to save {interestSaved:C} in interest"
                + (monthsSaved > 0 ? $" and finish about {monthsSaved} month(s) sooner" : "")
                + " versus continuing your current monthly card payments without a loan.";
        }
        else if (interestSaved < 0)
        {
            summary =
                $"Taking this loan is estimated to cost {Math.Abs(interestSaved):C} more in interest"
                + (monthsSaved < 0 ? $" and take about {Math.Abs(monthsSaved)} more month(s)" : "")
                + " than continuing your current monthly card payments without a loan.";
        }
        else
        {
            summary =
                "Interest is about the same with or without the loan at this payment level"
                + (monthsSaved != 0
                    ? $", but the loan path differs by about {Math.Abs(monthsSaved)} month(s)."
                    : ".");
        }

        var warnings = withoutLoan.Warnings
            .Concat(withLoan.Warnings)
            .Concat(loan.ScheduleWarnings)
            .Distinct()
            .ToList();
        if (excluded.Count > 0)
        {
            var names = string.Join(", ", excluded.Select(a => a.Name));
            warnings.Add(
                excluded.Count == 1
                    ? $"{names} is excluded from payoff analysis."
                    : $"{excluded.Count} cards are excluded from payoff analysis: {names}.");
        }

        var result = new CompareLoanSavingsResultDto
        {
            WithoutLoan = withoutDto,
            WithLoan = withDto,
            InterestSaved = interestSaved,
            MonthsSaved = monthsSaved,
            TotalPaidSaved = totalPaidSaved,
            Summary = summary
        };

        return ApiResponse<CalculationEnvelope<CompareLoanSavingsResultDto>>.Ok(
            CalculationEnvelope<CompareLoanSavingsResultDto>.Create(
                result,
                [
                    ..DefaultAssumptions,
                    "Without-loan scenario keeps your current monthly card payment budget and does not borrow.",
                    "With-loan scenario applies loan proceeds first, then pays the loan plus remaining card balances using your current budget plus the loan's required monthly payment."
                ],
                warnings));
    }

    private static LoanSavingsScenarioDto ToLoanSavingsScenario(string label, PayoffPlanResult plan) =>
        new()
        {
            Label = label,
            Strategy = PayoffStrategyNames.ToDisplayName(plan.Strategy),
            TotalInterest = plan.TotalInterestPaid,
            TotalPrincipalPaid = plan.TotalPrincipalPaid,
            TotalPaid = CreditCardMath.RoundMoney(plan.TotalInterestPaid + plan.TotalPrincipalPaid),
            MonthsToPayoff = plan.MonthsToPayoff,
            EstimatedPayoffDate = plan.OverallDebtFreeDate,
            IsValid = plan.IsValid,
            Warnings = plan.Warnings
        };

    public async Task<ApiResponse<IReadOnlyList<SavedPayoffPlanDto>>> ListSavedPayoffPlansAsync(
        CancellationToken cancellationToken = default)
    {
        var plans = await savedPayoffPlanRepository.ListAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<SavedPayoffPlanDto>>.Ok(
            plans.Select(ToSavedPayoffPlanDto).ToList());
    }

    public async Task<ApiResponse<SavedPayoffPlanDto>> CreateSavedPayoffPlanAsync(
        SavePayoffPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var strategy = CreateForecastRequestValidator.ParseStrategy(request.Strategy);
        if (strategy is null)
        {
            return ApiResponse<SavedPayoffPlanDto>.Fail(
                "Strategy must be Avalanche, Snowball, or MinimumsOnly.");
        }

        var now = DateTime.UtcNow;
        var plan = new SavedPayoffPlan
        {
            Name = request.Name.Trim(),
            Goal = NormalizeGoal(request.Goal),
            Strategy = PayoffStrategyNames.ToDisplayName(strategy.Value),
            ExtraMonthlyPayment = CreditCardMath.RoundMoney(request.ExtraMonthlyPayment),
            TotalMonthlyDebtPayment = CreditCardMath.RoundMoney(request.TotalMonthlyDebtPayment),
            TargetUtilizationPercent = request.TargetUtilizationPercent is > 0 and <= 99
                ? request.TargetUtilizationPercent
                : null,
            PayOverLimitFirst = request.PayOverLimitFirst,
            PostUtilizationStrategy = NormalizePostUtilizationStrategyLabel(request.PostUtilizationStrategy),
            EnableCashAdvanceBalanceMoves = request.EnableCashAdvanceBalanceMoves,
            LoanAmount = request.LoanAmount is > 0 ? CreditCardMath.RoundMoney(request.LoanAmount.Value) : null,
            LoanAnnualPercentageRate = request.LoanAmount is > 0
                ? Math.Max(0m, request.LoanAnnualPercentageRate ?? 0m)
                : null,
            LoanApplyStrategy = request.LoanAmount is > 0
                ? NormalizeLoanApplyStrategyLabel(request.LoanApplyStrategy, request.LoanApplyCreditCardIds)
                : null,
            LoanApplyCreditCardIdsJson = request.LoanAmount is > 0
                ? SerializeLoanApplyCreditCardIds(request.LoanApplyStrategy, request.LoanApplyCreditCardIds)
                : null,
            LoanType = request.LoanAmount is > 0
                ? LoanScheduleRequestValidator.ToLoanTypeLabel(
                    LoanScheduleRequestValidator.ParseLoanType(request.LoanType) ?? LoanType.Personal)
                : null,
            LoanTermMonths = request.LoanAmount is > 0 ? request.LoanTermMonths : null,
            LoanInterestOnlyMonths = request.LoanAmount is > 0 ? request.LoanInterestOnlyMonths : null,
            LoanFixedMonthlyPayment = request.LoanAmount is > 0 && request.LoanFixedMonthlyPayment is > 0
                ? CreditCardMath.RoundMoney(request.LoanFixedMonthlyPayment.Value)
                : null,
            PromotionalTransfersJson = SerializePromotionalTransfers(request.PromotionalTransfers),
            CreatedOnUtc = now,
            UpdatedOnUtc = now
        };

        await savedPayoffPlanRepository.AddAsync(plan, cancellationToken);
        await savedPayoffPlanRepository.SaveChangesAsync(cancellationToken);
        return ApiResponse<SavedPayoffPlanDto>.Ok(ToSavedPayoffPlanDto(plan));
    }

    public async Task<ApiResponse<SavedPayoffPlanDto>> UpdateSavedPayoffPlanAsync(
        int savedPayoffPlanId,
        UpdateSavedPayoffPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var plan = await savedPayoffPlanRepository.GetByIdAsync(savedPayoffPlanId, cancellationToken);
        if (plan is null)
        {
            return ApiResponse<SavedPayoffPlanDto>.Fail("Saved payoff plan was not found.");
        }

        var strategy = CreateForecastRequestValidator.ParseStrategy(request.Strategy);
        if (strategy is null)
        {
            return ApiResponse<SavedPayoffPlanDto>.Fail(
                "Strategy must be Avalanche, Snowball, or MinimumsOnly.");
        }

        plan.Name = request.Name.Trim();
        plan.Goal = NormalizeGoal(request.Goal);
        plan.Strategy = PayoffStrategyNames.ToDisplayName(strategy.Value);
        plan.ExtraMonthlyPayment = CreditCardMath.RoundMoney(request.ExtraMonthlyPayment);
        plan.TotalMonthlyDebtPayment = CreditCardMath.RoundMoney(request.TotalMonthlyDebtPayment);
        plan.TargetUtilizationPercent = request.TargetUtilizationPercent is > 0 and <= 99
            ? request.TargetUtilizationPercent
            : null;
        plan.PayOverLimitFirst = request.PayOverLimitFirst;
        plan.PostUtilizationStrategy = NormalizePostUtilizationStrategyLabel(request.PostUtilizationStrategy);
        plan.EnableCashAdvanceBalanceMoves = request.EnableCashAdvanceBalanceMoves;
        plan.LoanAmount = request.LoanAmount is > 0 ? CreditCardMath.RoundMoney(request.LoanAmount.Value) : null;
        plan.LoanAnnualPercentageRate = request.LoanAmount is > 0
            ? Math.Max(0m, request.LoanAnnualPercentageRate ?? 0m)
            : null;
        plan.LoanApplyStrategy = request.LoanAmount is > 0
            ? NormalizeLoanApplyStrategyLabel(request.LoanApplyStrategy, request.LoanApplyCreditCardIds)
            : null;
        plan.LoanApplyCreditCardIdsJson = request.LoanAmount is > 0
            ? SerializeLoanApplyCreditCardIds(request.LoanApplyStrategy, request.LoanApplyCreditCardIds)
            : null;
        plan.LoanType = request.LoanAmount is > 0
            ? LoanScheduleRequestValidator.ToLoanTypeLabel(
                LoanScheduleRequestValidator.ParseLoanType(request.LoanType) ?? LoanType.Personal)
            : null;
        plan.LoanTermMonths = request.LoanAmount is > 0 ? request.LoanTermMonths : null;
        plan.LoanInterestOnlyMonths = request.LoanAmount is > 0 ? request.LoanInterestOnlyMonths : null;
        plan.LoanFixedMonthlyPayment = request.LoanAmount is > 0 && request.LoanFixedMonthlyPayment is > 0
            ? CreditCardMath.RoundMoney(request.LoanFixedMonthlyPayment.Value)
            : null;
        plan.PromotionalTransfersJson = SerializePromotionalTransfers(request.PromotionalTransfers);
        plan.UpdatedOnUtc = DateTime.UtcNow;

        await savedPayoffPlanRepository.SaveChangesAsync(cancellationToken);
        return ApiResponse<SavedPayoffPlanDto>.Ok(ToSavedPayoffPlanDto(plan));
    }

    public async Task<ApiResponse<object?>> DeleteSavedPayoffPlanAsync(
        int savedPayoffPlanId,
        CancellationToken cancellationToken = default)
    {
        var plan = await savedPayoffPlanRepository.GetByIdAsync(savedPayoffPlanId, cancellationToken);
        if (plan is null)
        {
            return ApiResponse<object?>.Fail("Saved payoff plan was not found.");
        }

        await savedPayoffPlanRepository.DeleteAsync(plan, cancellationToken);
        await savedPayoffPlanRepository.SaveChangesAsync(cancellationToken);
        return ApiResponse<object?>.Ok(null);
    }

    public async Task<ApiResponse<CalculationEnvelope<CompareSavedPayoffPlansResultDto>>> CompareSavedPayoffPlansAsync(
        CompareSavedPayoffPlansRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PlanIds.Count is 0 or > CompareSavedPayoffPlansRequestValidator.MaxPlans)
        {
            return ApiResponse<CalculationEnvelope<CompareSavedPayoffPlansResultDto>>.Fail(
                $"Select between 1 and {CompareSavedPayoffPlansRequestValidator.MaxPlans} saved plans to compare.");
        }

        var plans = await savedPayoffPlanRepository.GetByIdsAsync(request.PlanIds, cancellationToken);
        if (plans.Count != request.PlanIds.Distinct().Count())
        {
            return ApiResponse<CalculationEnvelope<CompareSavedPayoffPlansResultDto>>.Fail(
                "One or more saved payoff plans were not found.");
        }

        var accounts = await LoadCreditCardsAsync(cancellationToken);
        var withBalance = accounts
            .Where(a => !a.IsPaidOff && a.Balance > 0)
            .ToList();
        var excluded = withBalance.Where(a => !IsIncludedInPayoffAnalysis(a)).ToList();
        var inputs = withBalance
            .Where(IsIncludedInPayoffAnalysis)
            .Select(ToPayoffInput)
            .ToList();

        if (inputs.Count == 0)
        {
            return ApiResponse<CalculationEnvelope<CompareSavedPayoffPlansResultDto>>.Fail(
                excluded.Count > 0
                    ? "No credit cards remain for payoff analysis after exclusions."
                    : "No active credit card balances were found to compare.");
        }

        var startDate = ToDateOnly(request.StartDate) ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var planById = plans.ToDictionary(p => p.SavedPayoffPlanId);
        var orderedPlans = request.PlanIds
            .Distinct()
            .Select(id => planById[id])
            .ToList();

        var warnings = new List<string>();
        if (excluded.Count > 0)
        {
            var names = string.Join(", ", excluded.Select(a => a.Name));
            warnings.Add(
                excluded.Count == 1
                    ? $"{names} is excluded from payoff analysis."
                    : $"{excluded.Count} cards are excluded from payoff analysis: {names}.");
        }

        var items = new List<SavedPayoffPlanCompareItemDto>();
        foreach (var plan in orderedPlans)
        {
            var strategy = CreateForecastRequestValidator.ParseStrategy(plan.Strategy);
            if (strategy is null)
            {
                return ApiResponse<CalculationEnvelope<CompareSavedPayoffPlansResultDto>>.Fail(
                    $"Saved plan '{plan.Name}' has an invalid strategy.");
            }

            var targetUtilization = plan.TargetUtilizationPercent is > 0 and <= 99
                ? plan.TargetUtilizationPercent
                : null;
            var promoTransfers = DeserializePromotionalTransfers(plan.PromotionalTransfersJson)
                .Where(t => t.FromCreditCardId != t.ToCreditCardId && t.PromotionalPeriodMonths > 0)
                .Select(t => new PromotionalBalanceTransferPlan(
                    t.FromCreditCardId,
                    t.ToCreditCardId,
                    t.Amount is > 0 ? t.Amount : null,
                    t.PromotionalAnnualPercentageRate,
                    t.PromotionalPeriodMonths,
                    Math.Max(0, t.ApplyAtMonthOffset)))
                .ToList();
            var postUtilizationStrategy = ParsePostUtilizationStrategy(plan.PostUtilizationStrategy);
            var loan = ResolveLoanPlanArgs(
                plan.LoanAmount,
                plan.LoanAnnualPercentageRate,
                plan.LoanApplyStrategy,
                DeserializeLoanApplyCreditCardIds(plan.LoanApplyCreditCardIdsJson),
                plan.LoanType,
                plan.LoanTermMonths,
                plan.LoanInterestOnlyMonths,
                plan.LoanFixedMonthlyPayment);
            var totalPayment = CreditCardMath.RoundMoney(
                plan.TotalMonthlyDebtPayment + loan.RequiredMonthlyBudget);

            var generated = payoffEngine.GeneratePlan(new PayoffPlanRequest(
                inputs,
                totalPayment,
                strategy.Value,
                startDate,
                targetUtilization,
                plan.PayOverLimitFirst,
                plan.EnableCashAdvanceBalanceMoves,
                promoTransfers,
                postUtilizationStrategy,
                loan.Amount,
                loan.Apr,
                loan.ApplyStrategy,
                loan.Type,
                loan.TermMonths,
                loan.InterestOnlyMonths,
                loan.FixedMonthlyPayment,
                loan.ApplyCreditCardIds));

            var minimums = payoffEngine.GeneratePlan(new PayoffPlanRequest(
                inputs,
                totalPayment,
                PayoffStrategyType.MinimumsOnly,
                startDate,
                LoanAmount: loan.Amount,
                LoanAnnualPercentageRate: loan.Apr,
                LoanApplyStrategy: loan.ApplyStrategy,
                LoanType: loan.Type,
                LoanTermMonths: loan.TermMonths,
                LoanInterestOnlyMonths: loan.InterestOnlyMonths,
                LoanFixedMonthlyPayment: loan.FixedMonthlyPayment,
                LoanApplyCreditCardIds: loan.ApplyCreditCardIds));

            var summary = ToStrategyDto(generated, minimums);
            items.Add(new SavedPayoffPlanCompareItemDto
            {
                SavedPayoffPlanId = plan.SavedPayoffPlanId,
                Name = plan.Name,
                StrategySummary = summary
            });
            warnings.AddRange(summary.Warnings);
        }

        var result = new CompareSavedPayoffPlansResultDto { Plans = items };
        return ApiResponse<CalculationEnvelope<CompareSavedPayoffPlansResultDto>>.Ok(
            CalculationEnvelope<CompareSavedPayoffPlansResultDto>.Create(
                result,
                DefaultAssumptions,
                warnings.Distinct().ToList()));
    }

    public async Task<ApiResponse<CalculationEnvelope<InterestAnalysisResultDto>>> AnalyzeInterestAsync(
        int creditCardId,
        InterestAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await accountRepository.GetByIdAndCategoryAsync(
            creditCardId,
            CreditCardCategory.CategoryId,
            cancellationToken);

        if (account is null)
        {
            return ApiResponse<CalculationEnvelope<InterestAnalysisResultDto>>.Fail(
                $"Credit card with id {creditCardId} was not found.");
        }

        var payment = request.MonthlyPayment
            ?? CreditCardMath.ResolveMinimumPayment(
                account.Balance,
                account.MonthlyPayment,
                account.CreditCardDetail?.MinimumPaymentPercentage,
                account.CreditCardDetail?.MinimumPaymentFloor);

        if (payment <= 0)
        {
            return ApiResponse<CalculationEnvelope<InterestAnalysisResultDto>>.Fail(
                "A monthly payment greater than zero is required for interest analysis.");
        }

        var startDate = ToDateOnly(request.StartDate) ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var calc = interestEngine.Calculate(new InterestCalculationRequest(
            Balance: account.Balance,
            AnnualPercentageRate: account.CreditCardDetail?.InterestRate ?? 0m,
            MonthlyPayment: payment,
            StartDate: startDate,
            PromotionalAnnualPercentageRate: account.CreditCardDetail?.PromotionalAnnualPercentageRate,
            PromotionalRateExpirationDate: ToDateOnly(account.CreditCardDetail?.PromotionalRateExpirationDate)));

        var warnings = new List<string>();
        if (calc.NegativeAmortizationDetected)
        {
            warnings.Add("Payment does not cover monthly interest; balance may not decline.");
        }

        var dto = new InterestAnalysisResultDto
        {
            CreditCardId = account.AccountId,
            Name = account.Name,
            DailyInterest = calc.DailyInterest,
            EstimatedMonthlyInterest = calc.EstimatedMonthlyInterest,
            EstimatedAnnualInterest = calc.EstimatedAnnualInterest,
            TotalInterestPaid = calc.TotalInterestPaid,
            TotalPrincipalPaid = calc.TotalPrincipalPaid,
            RemainingBalance = calc.RemainingBalance,
            EstimatedPayoffDate = calc.EstimatedPayoffDate,
            NumberOfPayments = calc.NumberOfPayments,
            NegativeAmortizationDetected = calc.NegativeAmortizationDetected
        };

        return ApiResponse<CalculationEnvelope<InterestAnalysisResultDto>>.Ok(
            CalculationEnvelope<InterestAnalysisResultDto>.Create(dto, DefaultAssumptions, warnings));
    }

    public Task<ApiResponse<CalculationEnvelope<BalanceTransferAnalysisResultDto>>> AnalyzeBalanceTransferAsync(
        AnalyzeBalanceTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startDate = ToDateOnly(request.StartDate) ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var calc = balanceTransferEngine.Analyze(new BalanceTransferAnalysisRequest(
            TransferAmount: request.TransferAmount,
            CurrentAnnualPercentageRate: request.CurrentAnnualPercentageRate,
            PromotionalAnnualPercentageRate: request.PromotionalAnnualPercentageRate,
            PromotionalPeriodMonths: request.PromotionalPeriodMonths,
            TransferFeePercentage: request.TransferFeePercentage,
            TransferFeeFlatAmount: request.TransferFeeFlatAmount,
            NewRegularAnnualPercentageRate: request.NewRegularAnnualPercentageRate,
            PlannedMonthlyPayment: request.PlannedMonthlyPayment,
            AvailableTransferLimit: request.AvailableTransferLimit,
            StartDate: startDate,
            IncludeFeeInTransferredBalance: request.IncludeFeeInTransferredBalance));

        if (!calc.IsValid)
        {
            return Task.FromResult(
                ApiResponse<CalculationEnvelope<BalanceTransferAnalysisResultDto>>.Fail(
                    calc.Explanation));
        }

        var dto = new BalanceTransferAnalysisResultDto
        {
            RequestedTransferAmount = calc.RequestedTransferAmount,
            AppliedTransferAmount = calc.AppliedTransferAmount,
            TotalTransferFee = calc.TotalTransferFee,
            StartingBalanceWithTransfer = calc.StartingBalanceWithTransfer,
            InterestWithoutTransfer = calc.InterestWithoutTransfer,
            InterestWithTransfer = calc.InterestWithTransfer,
            NetSavings = calc.NetSavings,
            BreakEvenMonth = calc.BreakEvenMonth,
            BalanceRemainingWhenPromotionEnds = calc.BalanceRemainingWhenPromotionEnds,
            PaymentNeededToClearBeforePromotionEnds = calc.PaymentNeededToClearBeforePromotionEnds,
            MonthsCompared = calc.MonthsCompared,
            Recommendation = BalanceTransferRecommendationNames.ToDisplayName(calc.Recommendation),
            Explanation = calc.Explanation
        };

        return Task.FromResult(
            ApiResponse<CalculationEnvelope<BalanceTransferAnalysisResultDto>>.Ok(
                CalculationEnvelope<BalanceTransferAnalysisResultDto>.Create(
                    dto,
                    BalanceTransferAssumptions,
                    calc.Warnings)));
    }

    public async Task<ApiResponse<CalculationEnvelope<CashFlowAnalysisResultDto>>> AnalyzeCashFlowAsync(
        AnalyzeCashFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        decimal debtMinimums;
        if (request.ExistingDebtMinimums is not null)
        {
            debtMinimums = request.ExistingDebtMinimums.Value;
        }
        else
        {
            var accounts = await LoadCreditCardsAsync(cancellationToken);
            debtMinimums = CreditCardMath.RoundMoney(
                accounts
                    .Where(a => !a.IsPaidOff && a.Balance > 0)
                    .Sum(a => CreditCardMath.ResolveMinimumPayment(
                        a.Balance,
                        a.MonthlyPayment,
                        a.CreditCardDetail?.MinimumPaymentPercentage,
                        a.CreditCardDetail?.MinimumPaymentFloor)));
        }

        var calc = cashFlowEngine.Calculate(new CashFlowRequest(
            MonthlyNetIncome: request.MonthlyNetIncome,
            RequiredExpenses: request.RequiredExpenses,
            VariableExpenses: request.VariableExpenses,
            ExistingDebtMinimums: debtMinimums,
            EmergencySavingsContribution: request.EmergencySavingsContribution,
            SafetyBuffer: request.SafetyBuffer,
            AdditionalAvailableFunds: request.AdditionalAvailableFunds,
            UserOverrideExtraPayment: request.UserOverrideExtraPayment));

        if (!calc.IsValid)
        {
            return ApiResponse<CalculationEnvelope<CashFlowAnalysisResultDto>>.Fail(
                calc.Warnings.FirstOrDefault() ?? "Cash-flow analysis could not be completed.");
        }

        var dto = new CashFlowAnalysisResultDto
        {
            MonthlyDisposableIncome = calc.MonthlyDisposableIncome,
            RequiredDebtMinimums = calc.RequiredDebtMinimums,
            SafeExtraDebtPayment = calc.SafeExtraDebtPayment,
            AggressiveExtraDebtPayment = calc.AggressiveExtraDebtPayment,
            RemainingCashBuffer = calc.RemainingCashBuffer,
            RecommendedExtraDebtPayment = calc.RecommendedExtraDebtPayment,
            UsedUserOverride = calc.UsedUserOverride,
            SuggestedTotalMonthlyDebtPayment = CreditCardMath.RoundMoney(
                calc.RequiredDebtMinimums + calc.RecommendedExtraDebtPayment)
        };

        return ApiResponse<CalculationEnvelope<CashFlowAnalysisResultDto>>.Ok(
            CalculationEnvelope<CashFlowAnalysisResultDto>.Create(
                dto,
                CashFlowAssumptions,
                calc.Warnings));
    }

    public async Task<ApiResponse<CalculationEnvelope<ForecastResultDto>>> CreateForecastAsync(
        CreateForecastRequest request,
        CancellationToken cancellationToken = default)
    {
        var strategy = CreateForecastRequestValidator.ParseStrategy(request.Strategy);
        if (strategy is null)
        {
            return ApiResponse<CalculationEnvelope<ForecastResultDto>>.Fail(
                "Strategy must be Avalanche, Snowball, or MinimumsOnly.");
        }

        var accounts = await LoadCreditCardsAsync(cancellationToken);
        var inputs = accounts
            .Where(a => !a.IsPaidOff && a.Balance > 0 && IsIncludedInPayoffAnalysis(a))
            .Select(ToPayoffInput)
            .ToList();

        if (inputs.Count == 0)
        {
            return ApiResponse<CalculationEnvelope<ForecastResultDto>>.Fail(
                "No active credit card balances were found to forecast.");
        }

        var startDate = ToDateOnly(request.StartDate) ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var targetUtilization = request.TargetUtilizationPercent is > 0 and <= 99
            ? request.TargetUtilizationPercent
            : null;

        var calc = forecastEngine.Generate(new ForecastRequest(
            CreditCards: inputs,
            Strategy: strategy.Value,
            TotalMonthlyDebtPayment: request.TotalMonthlyDebtPayment,
            StartDate: startDate,
            ForecastMonths: request.ForecastMonths,
            MonthlyNetIncome: request.MonthlyNetIncome ?? 0m,
            MonthlyExpenses: request.MonthlyExpenses ?? 0m,
            TargetUtilizationPercent: targetUtilization,
            PayOverLimitFirst: request.PayOverLimitFirst,
            AdditionalCharges: request.AdditionalCharges?
                .Select(c => new ForecastCharge(c.MonthOffset, c.Amount, c.CreditCardId))
                .ToList(),
            OneTimePayments: request.OneTimePayments?
                .Select(p => new ForecastOneTimePayment(p.MonthOffset, p.Amount))
                .ToList(),
            PaymentOverrides: request.PaymentOverrides?
                .Select(p => new ForecastPaymentOverride(p.MonthOffset, p.TotalMonthlyDebtPayment))
                .ToList(),
            IncomeChanges: request.IncomeChanges?
                .Select(c => new ForecastIncomeChange(c.MonthOffset, c.MonthlyNetIncomeDelta))
                .ToList(),
            ExpenseChanges: request.ExpenseChanges?
                .Select(c => new ForecastExpenseChange(c.MonthOffset, c.MonthlyExpenseDelta))
                .ToList()));

        if (!calc.IsValid)
        {
            return ApiResponse<CalculationEnvelope<ForecastResultDto>>.Fail(
                calc.Warnings.FirstOrDefault() ?? "Forecast could not be generated.");
        }

        int? forecastId = null;
        string? name = null;
        if (request.Save)
        {
            var scenario = new ForecastScenario
            {
                Name = request.Name!.Trim(),
                Strategy = PayoffStrategyNames.ToDisplayName(calc.Strategy),
                TotalMonthlyDebtPayment = calc.TotalMonthlyDebtPayment,
                StartDate = startDate,
                ForecastMonths = calc.ForecastMonths,
                StartingDebt = calc.StartingDebt,
                MonthlyNetIncome = request.MonthlyNetIncome,
                MonthlyExpenses = request.MonthlyExpenses,
                TargetUtilizationPercent = targetUtilization,
                PayOverLimitFirst = request.PayOverLimitFirst,
                EstimatedDebtFreeDate = calc.EstimatedDebtFreeDate,
                TotalInterestPaid = calc.TotalInterestPaid,
                CreatedOnUtc = DateTime.UtcNow,
                CreditCards = inputs.Select(c => new ForecastScenarioCreditCard
                {
                    CreditCardId = c.CreditCardId,
                    Name = c.Name,
                    StartingBalance = c.CurrentBalance,
                    CreditLimit = c.CreditLimit,
                    AnnualPercentageRate = c.AnnualPercentageRate
                }).ToList(),
                MonthlySnapshots = calc.Months.Select(m => new ForecastMonthlySnapshot
                {
                    Month = m.Month,
                    MonthIndex = m.MonthIndex,
                    StartingDebt = m.StartingDebt,
                    NewCharges = m.NewCharges,
                    Interest = m.Interest,
                    Payments = m.Payments,
                    EndingDebt = m.EndingDebt,
                    TotalCreditLimit = m.TotalCreditLimit,
                    OverallUtilizationPercentage = m.OverallUtilizationPercentage,
                    AvailableCash = m.AvailableCash,
                    CardsPaidOffThisMonth = m.CardsPaidOffThisMonth,
                    CumulativeInterest = m.CumulativeInterest
                }).ToList()
            };

            await forecastScenarioRepository.AddAsync(scenario, cancellationToken);
            await forecastScenarioRepository.SaveChangesAsync(cancellationToken);
            forecastId = scenario.ForecastScenarioId;
            name = scenario.Name;
        }

        var dto = ToForecastDto(calc, forecastId, name);
        return ApiResponse<CalculationEnvelope<ForecastResultDto>>.Ok(
            CalculationEnvelope<ForecastResultDto>.Create(dto, ForecastAssumptions, calc.Warnings));
    }

    public async Task<ApiResponse<CalculationEnvelope<ForecastResultDto>>> GetForecastAsync(
        int forecastId,
        CancellationToken cancellationToken = default)
    {
        var scenario = await forecastScenarioRepository.GetByIdAsync(forecastId, cancellationToken);
        if (scenario is null)
        {
            return ApiResponse<CalculationEnvelope<ForecastResultDto>>.Fail(
                $"Forecast with id {forecastId} was not found.");
        }

        var dto = new ForecastResultDto
        {
            ForecastId = scenario.ForecastScenarioId,
            Name = scenario.Name,
            Strategy = scenario.Strategy,
            StartingDebt = scenario.StartingDebt,
            MonthlyPayment = scenario.TotalMonthlyDebtPayment,
            ForecastMonths = scenario.ForecastMonths,
            EstimatedDebtFreeDate = scenario.EstimatedDebtFreeDate,
            TotalInterestPaid = scenario.TotalInterestPaid,
            Months = scenario.MonthlySnapshots
                .OrderBy(m => m.MonthIndex)
                .Select(m => new ForecastMonthDto
                {
                    Month = m.Month,
                    MonthIndex = m.MonthIndex,
                    StartingDebt = m.StartingDebt,
                    NewCharges = m.NewCharges,
                    Interest = m.Interest,
                    Payments = m.Payments,
                    EndingDebt = m.EndingDebt,
                    TotalCreditLimit = m.TotalCreditLimit,
                    OverallUtilizationPercentage = m.OverallUtilizationPercentage,
                    AvailableCash = m.AvailableCash,
                    CardsPaidOffThisMonth = m.CardsPaidOffThisMonth,
                    CumulativeInterest = m.CumulativeInterest
                })
                .ToList()
        };

        return ApiResponse<CalculationEnvelope<ForecastResultDto>>.Ok(
            CalculationEnvelope<ForecastResultDto>.Create(dto, ForecastAssumptions));
    }

    public async Task<ApiResponse<object?>> DeleteForecastAsync(
        int forecastId,
        CancellationToken cancellationToken = default)
    {
        var scenario = await forecastScenarioRepository.GetByIdAsync(forecastId, cancellationToken);
        if (scenario is null)
        {
            return ApiResponse<object?>.Fail($"Forecast with id {forecastId} was not found.");
        }

        await forecastScenarioRepository.DeleteAsync(scenario, cancellationToken);
        await forecastScenarioRepository.SaveChangesAsync(cancellationToken);
        return ApiResponse<object?>.Ok(null);
    }

    public async Task<ApiResponse<CalculationEnvelope<UtilizationSummaryResultDto>>> GetUtilizationSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await LoadCreditCardsAsync(cancellationToken);
        var inputs = accounts
            .Where(a => !a.IsPaidOff)
            .Select(a => new CreditCardUtilizationInput(
                a.AccountId,
                a.Name,
                a.Balance,
                a.Limit))
            .ToList();

        var calc = utilizationEngine.Calculate(inputs);
        var dto = new UtilizationSummaryResultDto
        {
            TotalBalances = calc.TotalBalances,
            TotalCreditLimits = calc.TotalCreditLimits,
            OverallUtilizationPercentage = calc.OverallUtilizationPercentage,
            Cards = calc.Cards.Select(c => new CardUtilizationDto
            {
                CreditCardId = c.CreditCardId,
                Name = c.Name,
                CurrentBalance = c.CurrentBalance,
                CreditLimit = c.CreditLimit,
                AvailableCredit = c.AvailableCredit,
                UtilizationPercentage = c.UtilizationPercentage,
                ThresholdTargets = c.ThresholdTargets.Select(t => new UtilizationThresholdDto
                {
                    ThresholdPercent = t.ThresholdPercent,
                    TargetBalance = t.TargetBalance,
                    PaymentRequired = t.PaymentRequired
                }).ToList()
            }).ToList(),
            OverallThresholdTargets = calc.OverallThresholdTargets.Select(t => new UtilizationThresholdDto
            {
                ThresholdPercent = t.ThresholdPercent,
                TargetBalance = t.TargetBalance,
                PaymentRequired = t.PaymentRequired
            }).ToList()
        };

        return ApiResponse<CalculationEnvelope<UtilizationSummaryResultDto>>.Ok(
            CalculationEnvelope<UtilizationSummaryResultDto>.Create(
                dto,
                [
                    "Utilization is calculated as balance divided by credit limit.",
                    "Credit-score effects are estimates only and are not guaranteed."
                ]));
    }

    public Task<ApiResponse<CalculationEnvelope<LoanScheduleResultDto>>> BuildLoanScheduleAsync(
        LoanScheduleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var type = LoanScheduleRequestValidator.ParseLoanType(request.LoanType);
        if (type is null)
        {
            return Task.FromResult(ApiResponse<CalculationEnvelope<LoanScheduleResultDto>>.Fail(
                "LoanType must be Personal, HomeEquity, Heloc, Retirement401k, or Family."));
        }

        var built = LoanRepaymentScheduleBuilder.Build(new LoanScheduleRequest(
            type.Value,
            request.Amount,
            request.AnnualPercentageRate,
            request.TermMonths,
            request.InterestOnlyMonths,
            request.FixedMonthlyPayment));

        var dto = ToLoanScheduleDto(built, LoanScheduleRequestValidator.ToLoanTypeLabel(type) ?? request.LoanType);
        if (!built.IsValid)
        {
            return Task.FromResult(ApiResponse<CalculationEnvelope<LoanScheduleResultDto>>.Fail(
                built.Errors));
        }

        return Task.FromResult(ApiResponse<CalculationEnvelope<LoanScheduleResultDto>>.Ok(
            CalculationEnvelope<LoanScheduleResultDto>.Create(
                dto,
                [
                    "Schedule models interest before each payment using a standard monthly rate (APR / 12).",
                    "HELOC schedules use interest-only payments during the draw period, then amortize the remaining balance.",
                    "Estimates exclude fees, taxes, and rate changes."
                ],
                [])));
    }

    private static LoanScheduleResultDto ToLoanScheduleDto(LoanScheduleResult built, string loanTypeLabel) =>
        new()
        {
            IsValid = built.IsValid,
            Errors = built.Errors,
            LoanType = loanTypeLabel,
            LoanTypeDisplayName = built.LoanTypeDisplayName,
            MonthlyPayment = built.MonthlyPayment,
            Phase2MonthlyPayment = built.Phase2MonthlyPayment,
            MonthsToPayoff = built.MonthsToPayoff,
            TotalInterest = built.TotalInterest,
            TotalPaid = built.TotalPaid,
            Schedule = built.Schedule.Select(s => new LoanScheduleMonthDto
            {
                MonthNumber = s.MonthNumber,
                Payment = s.Payment,
                Interest = s.Interest,
                Principal = s.Principal,
                EndingBalance = s.EndingBalance
            }).ToList()
        };

    private readonly record struct LoanPlanArgs(
        decimal? Amount,
        decimal? Apr,
        PayoffStrategyType? ApplyStrategy,
        LoanType? Type,
        int? TermMonths,
        int? InterestOnlyMonths,
        decimal? FixedMonthlyPayment,
        decimal RequiredMonthlyBudget,
        IReadOnlyList<string> ScheduleWarnings,
        IReadOnlyList<int>? ApplyCreditCardIds);

    private static LoanPlanArgs ResolveLoanPlanArgs(
        decimal? loanAmount,
        decimal? loanApr,
        string? loanApplyStrategy,
        IReadOnlyList<int>? loanApplyCreditCardIds,
        string? loanType,
        int? termMonths,
        int? interestOnlyMonths,
        decimal? fixedMonthlyPayment)
    {
        if (loanAmount is not > 0)
        {
            return new LoanPlanArgs(null, null, null, null, null, null, null, 0m, [], null);
        }

        var type = LoanScheduleRequestValidator.ParseLoanType(loanType) ?? LoanType.Personal;
        var apr = Math.Max(0m, loanApr ?? 0m);
        var schedule = LoanRepaymentScheduleBuilder.Build(new LoanScheduleRequest(
            type,
            loanAmount.Value,
            apr,
            termMonths,
            interestOnlyMonths,
            fixedMonthlyPayment));

        if (!schedule.IsValid)
        {
            return new LoanPlanArgs(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                0m,
                schedule.Errors.ToList(),
                null);
        }

        var selectedIds = NormalizeLoanApplyCreditCardIds(loanApplyStrategy, loanApplyCreditCardIds);
        PayoffStrategyType? applyStrategy = selectedIds is { Count: > 0 }
            ? null
            : ParsePostUtilizationStrategy(loanApplyStrategy) ?? PayoffStrategyType.Avalanche;

        return new LoanPlanArgs(
            CreditCardMath.RoundMoney(loanAmount.Value),
            apr,
            applyStrategy,
            type,
            termMonths,
            interestOnlyMonths,
            fixedMonthlyPayment is > 0 ? CreditCardMath.RoundMoney(fixedMonthlyPayment.Value) : null,
            LoanRepaymentScheduleBuilder.RequiredMonthlyBudget(schedule),
            [],
            selectedIds);
    }

    private static IReadOnlyList<int>? NormalizeLoanApplyCreditCardIds(
        string? loanApplyStrategy,
        IReadOnlyList<int>? ids)
    {
        if (!string.Equals(loanApplyStrategy, "SelectedAccounts", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var cleaned = (ids ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        return cleaned.Count > 0 ? cleaned : null;
    }

    private static string NormalizeLoanApplyStrategyLabel(
        string? value,
        IReadOnlyList<int>? ids)
    {
        _ = ids;
        if (string.Equals(value, "SelectedAccounts", StringComparison.OrdinalIgnoreCase))
        {
            return "SelectedAccounts";
        }

        return NormalizePostUtilizationStrategyLabel(value) ?? "Avalanche";
    }

    private static string? SerializeLoanApplyCreditCardIds(
        string? strategy,
        IReadOnlyList<int>? ids)
    {
        var cleaned = NormalizeLoanApplyCreditCardIds(strategy, ids);
        if (cleaned is null || cleaned.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(cleaned, PromoJsonOptions);
    }

    private static IReadOnlyList<int> DeserializeLoanApplyCreditCardIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<int>>(json, PromoJsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task<IReadOnlyList<Account>> LoadCreditCardsAsync(CancellationToken cancellationToken) =>
        await accountRepository.GetByCategoryAsync(CreditCardCategory.CategoryId, cancellationToken);

    /// <summary>Missing detail defaults to included so existing cards stay in analysis.</summary>
    private static bool IsIncludedInPayoffAnalysis(Account account) =>
        account.CreditCardDetail?.IncludeInPayoffAnalysis ?? true;

    private static PayoffStrategyType? ParsePostUtilizationStrategy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "avalanche" => PayoffStrategyType.Avalanche,
            "snowball" => PayoffStrategyType.Snowball,
            _ => null
        };
    }

    private static CreditCardPayoffInput ToPayoffInput(Account account) =>
        new(
            CreditCardId: account.AccountId,
            Name: account.Name,
            CurrentBalance: account.Balance,
            CreditLimit: account.Limit,
            AnnualPercentageRate: account.CreditCardDetail?.InterestRate ?? 0m,
            FixedMonthlyPayment: account.MonthlyPayment,
            MinimumPaymentPercentage: account.CreditCardDetail?.MinimumPaymentPercentage,
            MinimumPaymentFloor: account.CreditCardDetail?.MinimumPaymentFloor,
            PromotionalAnnualPercentageRate: account.CreditCardDetail?.PromotionalAnnualPercentageRate,
            PromotionalRateExpirationDate: ToDateOnly(account.CreditCardDetail?.PromotionalRateExpirationDate),
            CashAdvanceInterestRate: account.CreditCardDetail?.CashOutInterestRate,
            CashAdvanceFeePercentage: account.CreditCardDetail?.CashAdvanceFeePercentage);

    private static PayoffStrategySummaryDto ToStrategyDto(PayoffPlanResult plan, PayoffPlanResult minimums)
    {
        // Only compare interest when minimums-only actually reaches payoff. Otherwise the
        // engine can run to the max horizon with growing balances and report nonsense savings.
        decimal? saved = null;
        if (plan.IsValid
            && plan.OverallDebtFreeDate is not null
            && minimums.OverallDebtFreeDate is not null
            && minimums.TotalInterestPaid > 0)
        {
            saved = CreditCardMath.RoundMoney(minimums.TotalInterestPaid - plan.TotalInterestPaid);
            if (saved <= 0)
            {
                saved = null;
            }
        }

        var scheduleByCard = plan.Schedule
            .GroupBy(s => s.CreditCardId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CardMonthlyBalanceDto>)g
                    .OrderBy(s => s.Month)
                    .Select(s => new CardMonthlyBalanceDto
                    {
                        Month = s.Month,
                        StartingBalance = s.StartingBalance,
                        InterestCharged = s.InterestCharged,
                        PaymentApplied = s.PaymentApplied,
                        MinimumPaymentApplied = s.MinimumPaymentApplied,
                        ExtraPaymentApplied = s.ExtraPaymentApplied,
                        PrincipalApplied = s.PrincipalApplied,
                        BalanceTransferredIn = s.BalanceTransferredIn,
                        BalanceTransferredOut = s.BalanceTransferredOut,
                        Transfers = s.Transfers.Select(t => new BalanceTransferLegDto
                        {
                            CounterpartyCreditCardId = t.CounterpartyCreditCardId,
                            CounterpartyName = t.CounterpartyName,
                            Amount = t.Amount,
                            Direction = t.Direction
                        }).ToList(),
                        EndingBalance = s.EndingBalance
                    })
                    .ToList());

        return new PayoffStrategySummaryDto
        {
            Strategy = PayoffStrategyNames.ToDisplayName(plan.Strategy),
            EstimatedPayoffDate = plan.OverallDebtFreeDate,
            TotalInterest = plan.TotalInterestPaid,
            MonthsToPayoff = plan.MonthsToPayoff,
            CombinedMinimumPayments = plan.CombinedMinimumPayments,
            IsValid = plan.IsValid,
            Warnings = plan.Warnings
                .Concat(saved is > 0
                    ? [$"Estimated interest saved versus minimums-only: {saved:C}."]
                    : Array.Empty<string>())
                .ToList(),
            CardOrder = plan.CardOrder.Select(c => new CardPayoffOrderDto
            {
                CreditCardId = c.CreditCardId,
                Name = c.Name,
                PriorityOrder = c.PriorityOrder,
                EstimatedPayoffDate = c.EstimatedPayoffDate,
                TotalInterestPaid = c.TotalInterestPaid,
                MonthlyBalances = scheduleByCard.GetValueOrDefault(c.CreditCardId, [])
            }).ToList()
        };
    }

    private static ForecastResultDto ToForecastDto(
        ForecastResult calc,
        int? forecastId,
        string? name) =>
        new()
        {
            ForecastId = forecastId,
            Name = name,
            Strategy = PayoffStrategyNames.ToDisplayName(calc.Strategy),
            StartingDebt = calc.StartingDebt,
            MonthlyPayment = calc.TotalMonthlyDebtPayment,
            ForecastMonths = calc.ForecastMonths,
            EstimatedDebtFreeDate = calc.EstimatedDebtFreeDate,
            TotalInterestPaid = calc.TotalInterestPaid,
            Months = calc.Months.Select(m => new ForecastMonthDto
            {
                Month = m.Month,
                MonthIndex = m.MonthIndex,
                StartingDebt = m.StartingDebt,
                NewCharges = m.NewCharges,
                Interest = m.Interest,
                Payments = m.Payments,
                EndingDebt = m.EndingDebt,
                TotalCreditLimit = m.TotalCreditLimit,
                OverallUtilizationPercentage = m.OverallUtilizationPercentage,
                AvailableCash = m.AvailableCash,
                CardsPaidOffThisMonth = m.CardsPaidOffThisMonth,
                CumulativeInterest = m.CumulativeInterest
            }).ToList()
        };

    private static DateOnly? ToDateOnly(DateTime? value) =>
        value.HasValue ? DateOnly.FromDateTime(value.Value.ToUniversalTime()) : null;

    private static SavedPayoffPlanDto ToSavedPayoffPlanDto(SavedPayoffPlan plan) =>
        new()
        {
            SavedPayoffPlanId = plan.SavedPayoffPlanId,
            Name = plan.Name,
            Goal = plan.Goal,
            Strategy = plan.Strategy,
            ExtraMonthlyPayment = plan.ExtraMonthlyPayment,
            TotalMonthlyDebtPayment = plan.TotalMonthlyDebtPayment,
            TargetUtilizationPercent = plan.TargetUtilizationPercent,
            PayOverLimitFirst = plan.PayOverLimitFirst,
            PostUtilizationStrategy = plan.PostUtilizationStrategy,
            EnableCashAdvanceBalanceMoves = plan.EnableCashAdvanceBalanceMoves,
            LoanAmount = plan.LoanAmount,
            LoanAnnualPercentageRate = plan.LoanAnnualPercentageRate,
            LoanApplyStrategy = plan.LoanApplyStrategy,
            LoanApplyCreditCardIds = DeserializeLoanApplyCreditCardIds(plan.LoanApplyCreditCardIdsJson),
            LoanType = plan.LoanType,
            LoanTermMonths = plan.LoanTermMonths,
            LoanInterestOnlyMonths = plan.LoanInterestOnlyMonths,
            LoanFixedMonthlyPayment = plan.LoanFixedMonthlyPayment,
            PromotionalTransfers = DeserializePromotionalTransfers(plan.PromotionalTransfersJson),
            CreatedOnUtc = plan.CreatedOnUtc,
            UpdatedOnUtc = plan.UpdatedOnUtc
        };

    private static string? NormalizeGoal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim() switch
        {
            "improveCredit" => "improveCredit",
            "lowerUtilization" => "lowerUtilization",
            "minimizeInterest" => "minimizeInterest",
            _ => null
        };
    }

    private static string? NormalizePostUtilizationStrategyLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "avalanche" => "Avalanche",
            "snowball" => "Snowball",
            _ => null
        };
    }

    private static readonly JsonSerializerOptions PromoJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static string? SerializePromotionalTransfers(
        IReadOnlyList<PromotionalBalanceTransferDto>? transfers)
    {
        if (transfers is null || transfers.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(transfers, PromoJsonOptions);
    }

    private static IReadOnlyList<PromotionalBalanceTransferDto> DeserializePromotionalTransfers(
        string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<PromotionalBalanceTransferDto>>(json, PromoJsonOptions)
                ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
