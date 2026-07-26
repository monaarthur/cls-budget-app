using System.Text.Json;
using CLS.Budget.Application.Common;
using CLS.Budget.Application.CreditCardEngine.Dtos;
using CLS.Budget.Application.CreditCardEngine.Validators;
using CLS.Budget.Application.CreditCards;
using CLS.Budget.Domain.CreditCardEngine;
using CLS.Budget.Domain.CreditCardEngine.Loan;
using CLS.Budget.Domain.CreditCardEngine.Payoff;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.CreditCardEngine;

public sealed partial class CreditCardDecisionService
{
    public async Task<ApiResponse<ActivePayoffPlanDto>> ActivatePayoffPlanAsync(
        ActivatePayoffPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var config = await ResolveActivationConfigAsync(request, cancellationToken);
        if (!config.Success)
        {
            return ApiResponse<ActivePayoffPlanDto>.Fail(config.Errors);
        }

        var knobs = config.Data!;
        var strategy = CreateForecastRequestValidator.ParseStrategy(knobs.Strategy);
        if (strategy is null)
        {
            return ApiResponse<ActivePayoffPlanDto>.Fail(
                "Strategy must be Avalanche, Snowball, or MinimumsOnly.");
        }

        var analysisCards = await LoadAnalysisCardsAsync(cancellationToken);
        if (analysisCards.Count == 0)
        {
            return ApiResponse<ActivePayoffPlanDto>.Fail(
                "No active credit card balances were found to start a plan.");
        }

        var startDate = ToDateOnly(request.StartDate) ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var inputs = analysisCards.Select(ToPayoffInput).ToList();
        var startingDebt = CreditCardMath.RoundMoney(inputs.Sum(c => c.CurrentBalance));
        var projection = RunPlanProjection(knobs, inputs, startDate);

        var now = DateTime.UtcNow;
        var existing = await activePayoffPlanRepository.GetActiveAsync(cancellationToken);
        if (existing is not null)
        {
            existing.Status = ActivePayoffPlanStatuses.Completed;
            existing.EndedOnUtc = now;
            existing.UpdatedOnUtc = now;
            await activePayoffPlanRepository.AddEventAsync(
                new PayoffPlanEvent
                {
                    ActivePayoffPlanId = existing.ActivePayoffPlanId,
                    EventType = PayoffPlanEventTypes.Completed,
                    Summary = "Previous active plan archived because a new plan was started.",
                    PayloadJson = JsonSerializer.Serialize(
                        new { reason = "replaced" },
                        PromoJsonOptions),
                    CreatedOnUtc = now
                },
                cancellationToken);
        }

        var plan = new ActivePayoffPlan
        {
            Name = knobs.Name,
            Status = ActivePayoffPlanStatuses.Active,
            SourceSavedPayoffPlanId = knobs.SourceSavedPayoffPlanId,
            StartedOnUtc = now,
            CurrentVersionNumber = 1,
            StartingDebt = startingDebt,
            Goal = knobs.Goal,
            Strategy = PayoffStrategyNames.ToDisplayName(strategy.Value),
            ExtraMonthlyPayment = knobs.ExtraMonthlyPayment,
            TotalMonthlyDebtPayment = knobs.TotalMonthlyDebtPayment,
            TargetUtilizationPercent = knobs.TargetUtilizationPercent,
            PayOverLimitFirst = knobs.PayOverLimitFirst,
            PostUtilizationStrategy = knobs.PostUtilizationStrategy,
            EnableCashAdvanceBalanceMoves = knobs.EnableCashAdvanceBalanceMoves,
            LoanAmount = knobs.LoanAmount,
            LoanAnnualPercentageRate = knobs.LoanAnnualPercentageRate,
            LoanApplyStrategy = knobs.LoanApplyStrategy,
            LoanApplyCreditCardIdsJson = knobs.LoanApplyCreditCardIdsJson,
            LoanType = knobs.LoanType,
            LoanTermMonths = knobs.LoanTermMonths,
            LoanInterestOnlyMonths = knobs.LoanInterestOnlyMonths,
            LoanFixedMonthlyPayment = knobs.LoanFixedMonthlyPayment,
            PromotionalTransfersJson = knobs.PromotionalTransfersJson,
            CreatedOnUtc = now,
            UpdatedOnUtc = now
        };

        var version = CreateVersionSnapshot(
            plan,
            versionNumber: 1,
            reason: string.IsNullOrWhiteSpace(request.Reason) ? "Plan started" : request.Reason.Trim(),
            snapshotDebt: startingDebt,
            projection: projection,
            createdOnUtc: now);

        plan.Versions.Add(version);
        plan.Events.Add(new PayoffPlanEvent
        {
            EventType = PayoffPlanEventTypes.Started,
            Summary = $"Started plan \"{plan.Name}\" with {startingDebt:C} debt.",
            PayloadJson = JsonSerializer.Serialize(
                new
                {
                    startingDebt,
                    strategy = plan.Strategy,
                    monthlyPayment = plan.TotalMonthlyDebtPayment,
                    versionNumber = 1
                },
                PromoJsonOptions),
            CreatedOnUtc = now
        });

        await activePayoffPlanRepository.AddAsync(plan, cancellationToken);
        await activePayoffPlanRepository.SaveChangesAsync(cancellationToken);

        var progress = await BuildProgressAsync(plan, analysisCards, cancellationToken);
        return ApiResponse<ActivePayoffPlanDto>.Ok(ToActivePayoffPlanDto(plan, progress));
    }

    public async Task<ApiResponse<ActivePayoffPlanDto>> GetActivePayoffPlanAsync(
        CancellationToken cancellationToken = default)
    {
        var plan = await activePayoffPlanRepository.GetActiveWithDetailsAsync(cancellationToken);
        if (plan is null)
        {
            return ApiResponse<ActivePayoffPlanDto>.Fail("No active payoff plan was found.");
        }

        var cards = await LoadAnalysisCardsAsync(cancellationToken);
        var progress = await BuildProgressAsync(plan, cards, cancellationToken);
        return ApiResponse<ActivePayoffPlanDto>.Ok(ToActivePayoffPlanDto(plan, progress));
    }

    public async Task<ApiResponse<ActivePayoffPlanDto>> ReviseActivePayoffPlanAsync(
        ReviseActivePayoffPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var plan = await activePayoffPlanRepository.GetActiveWithDetailsAsync(cancellationToken);
        if (plan is null)
        {
            return ApiResponse<ActivePayoffPlanDto>.Fail("No active payoff plan was found.");
        }

        var strategy = CreateForecastRequestValidator.ParseStrategy(request.Strategy);
        if (strategy is null)
        {
            return ApiResponse<ActivePayoffPlanDto>.Fail(
                "Strategy must be Avalanche, Snowball, or MinimumsOnly.");
        }

        var now = DateTime.UtcNow;
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
        plan.LoanAmount = request.LoanAmount is > 0
            ? CreditCardMath.RoundMoney(request.LoanAmount.Value)
            : null;
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
        plan.CurrentVersionNumber += 1;
        plan.UpdatedOnUtc = now;

        var cards = await LoadAnalysisCardsAsync(cancellationToken);
        var inputs = cards.Select(ToPayoffInput).ToList();
        var snapshotDebt = CreditCardMath.RoundMoney(inputs.Sum(c => c.CurrentBalance));
        var knobs = PlanKnobs.FromActive(plan);
        var projection = RunPlanProjection(knobs, inputs, DateOnly.FromDateTime(DateTime.UtcNow));
        var version = CreateVersionSnapshot(
            plan,
            plan.CurrentVersionNumber,
            string.IsNullOrWhiteSpace(request.Reason) ? "Plan revised" : request.Reason.Trim(),
            snapshotDebt,
            projection,
            now);

        await activePayoffPlanRepository.AddVersionAsync(version, cancellationToken);
        await activePayoffPlanRepository.AddEventAsync(
            new PayoffPlanEvent
            {
                ActivePayoffPlanId = plan.ActivePayoffPlanId,
                EventType = PayoffPlanEventTypes.Revised,
                Summary =
                    $"Revised to version {plan.CurrentVersionNumber}"
                    + (string.IsNullOrWhiteSpace(request.Reason)
                        ? "."
                        : $": {request.Reason.Trim()}"),
                PayloadJson = JsonSerializer.Serialize(
                    new
                    {
                        versionNumber = plan.CurrentVersionNumber,
                        strategy = plan.Strategy,
                        monthlyPayment = plan.TotalMonthlyDebtPayment,
                        snapshotDebt
                    },
                    PromoJsonOptions),
                CreatedOnUtc = now
            },
            cancellationToken);

        await activePayoffPlanRepository.SaveChangesAsync(cancellationToken);

        var progress = await BuildProgressAsync(plan, cards, cancellationToken);
        return ApiResponse<ActivePayoffPlanDto>.Ok(ToActivePayoffPlanDto(plan, progress));
    }

    public async Task<ApiResponse<PayoffPlanPaymentDto>> RecordActivePayoffPlanPaymentAsync(
        RecordPayoffPlanPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var plan = await activePayoffPlanRepository.GetActiveWithDetailsAsync(cancellationToken);
        if (plan is null)
        {
            return ApiResponse<PayoffPlanPaymentDto>.Fail("No active payoff plan was found.");
        }

        if (request.Amount <= 0)
        {
            return ApiResponse<PayoffPlanPaymentDto>.Fail("Payment amount must be greater than zero.");
        }

        var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null || account.AccountCategoryId != CreditCardCategory.CategoryId)
        {
            return ApiResponse<PayoffPlanPaymentDto>.Fail("Credit card account was not found.");
        }

        var currentVersion = plan.Versions
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();
        if (currentVersion is null)
        {
            return ApiResponse<PayoffPlanPaymentDto>.Fail("Active plan is missing a version snapshot.");
        }

        var amount = CreditCardMath.RoundMoney(request.Amount);
        var applied = ApplyPaymentToAccount(account, amount);
        var paymentDate = ToDateOnly(request.PaymentDate) ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        var payment = new PayoffPlanPayment
        {
            ActivePayoffPlanId = plan.ActivePayoffPlanId,
            PayoffPlanVersionId = currentVersion.PayoffPlanVersionId,
            AccountId = account.AccountId,
            Amount = amount,
            PaymentDate = paymentDate,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedOnUtc = now
        };

        await activePayoffPlanRepository.AddPaymentAsync(payment, cancellationToken);
        await activePayoffPlanRepository.AddEventAsync(
            new PayoffPlanEvent
            {
                ActivePayoffPlanId = plan.ActivePayoffPlanId,
                EventType = PayoffPlanEventTypes.PaymentRecorded,
                Summary =
                    $"Recorded {amount:C} payment on {account.Name}"
                    + (applied < amount ? $" (applied {applied:C}; balance floored at $0)." : "."),
                PayloadJson = JsonSerializer.Serialize(
                    new
                    {
                        accountId = account.AccountId,
                        accountName = account.Name,
                        amount,
                        applied,
                        paymentDate,
                        versionNumber = currentVersion.VersionNumber,
                        newBalance = account.Balance,
                        isPaidOff = account.IsPaidOff
                    },
                    PromoJsonOptions),
                CreatedOnUtc = now
            },
            cancellationToken);

        plan.UpdatedOnUtc = now;
        await activePayoffPlanRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<PayoffPlanPaymentDto>.Ok(new PayoffPlanPaymentDto
        {
            PayoffPlanPaymentId = payment.PayoffPlanPaymentId,
            AccountId = payment.AccountId,
            AccountName = account.Name,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            Notes = payment.Notes,
            PayoffPlanVersionId = payment.PayoffPlanVersionId,
            VersionNumber = currentVersion.VersionNumber,
            IsVoided = false,
            CreatedOnUtc = payment.CreatedOnUtc
        });
    }

    public async Task<ApiResponse<object?>> VoidActivePayoffPlanPaymentAsync(
        int payoffPlanPaymentId,
        CancellationToken cancellationToken = default)
    {
        var plan = await activePayoffPlanRepository.GetActiveAsync(cancellationToken);
        if (plan is null)
        {
            return ApiResponse<object?>.Fail("No active payoff plan was found.");
        }

        var payment = await activePayoffPlanRepository.GetPaymentAsync(
            payoffPlanPaymentId,
            cancellationToken);
        if (payment is null || payment.ActivePayoffPlanId != plan.ActivePayoffPlanId)
        {
            return ApiResponse<object?>.Fail("Payment was not found on the active plan.");
        }

        if (payment.IsVoided)
        {
            return ApiResponse<object?>.Fail("Payment is already voided.");
        }

        var account = await accountRepository.GetByIdAsync(payment.AccountId, cancellationToken);
        if (account is null)
        {
            return ApiResponse<object?>.Fail("Credit card account was not found.");
        }

        RestorePaymentToAccount(account, payment.Amount);
        var now = DateTime.UtcNow;
        payment.IsVoided = true;
        payment.VoidedOnUtc = now;
        plan.UpdatedOnUtc = now;

        await activePayoffPlanRepository.AddEventAsync(
            new PayoffPlanEvent
            {
                ActivePayoffPlanId = plan.ActivePayoffPlanId,
                EventType = PayoffPlanEventTypes.PaymentVoided,
                Summary = $"Voided {payment.Amount:C} payment on {account.Name}.",
                PayloadJson = JsonSerializer.Serialize(
                    new
                    {
                        paymentId = payment.PayoffPlanPaymentId,
                        accountId = account.AccountId,
                        accountName = account.Name,
                        amount = payment.Amount,
                        newBalance = account.Balance
                    },
                    PromoJsonOptions),
                CreatedOnUtc = now
            },
            cancellationToken);

        await activePayoffPlanRepository.SaveChangesAsync(cancellationToken);
        return ApiResponse<object?>.Ok(null);
    }

    public async Task<ApiResponse<ActivePayoffPlanDto>> CompleteActivePayoffPlanAsync(
        CancellationToken cancellationToken = default) =>
        await EndActivePlanAsync(ActivePayoffPlanStatuses.Completed, PayoffPlanEventTypes.Completed,
            "Plan marked completed.", cancellationToken);

    public async Task<ApiResponse<ActivePayoffPlanDto>> AbandonActivePayoffPlanAsync(
        CancellationToken cancellationToken = default) =>
        await EndActivePlanAsync(ActivePayoffPlanStatuses.Abandoned, PayoffPlanEventTypes.Abandoned,
            "Plan abandoned.", cancellationToken);

    public async Task<ApiResponse<ActivePayoffPlanHistoryDto>> GetActivePayoffPlanHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        var plan = await activePayoffPlanRepository.GetActiveWithDetailsAsync(cancellationToken);
        if (plan is null)
        {
            return ApiResponse<ActivePayoffPlanHistoryDto>.Fail("No active payoff plan was found.");
        }

        var accountIds = plan.Payments.Select(p => p.AccountId).Distinct().ToList();
        var accounts = await accountRepository.GetByIdsAsync(accountIds, cancellationToken);
        var names = accounts.ToDictionary(a => a.AccountId, a => a.Name);
        var versionById = plan.Versions.ToDictionary(v => v.PayoffPlanVersionId);

        return ApiResponse<ActivePayoffPlanHistoryDto>.Ok(new ActivePayoffPlanHistoryDto
        {
            ActivePayoffPlanId = plan.ActivePayoffPlanId,
            Name = plan.Name,
            Status = plan.Status,
            Versions = plan.Versions
                .OrderBy(v => v.VersionNumber)
                .Select(ToVersionDto)
                .ToList(),
            Payments = plan.Payments
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.CreatedOnUtc)
                .Select(p => new PayoffPlanPaymentDto
                {
                    PayoffPlanPaymentId = p.PayoffPlanPaymentId,
                    AccountId = p.AccountId,
                    AccountName = names.GetValueOrDefault(p.AccountId),
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    Notes = p.Notes,
                    PayoffPlanVersionId = p.PayoffPlanVersionId,
                    VersionNumber = versionById.TryGetValue(p.PayoffPlanVersionId, out var v)
                        ? v.VersionNumber
                        : 0,
                    IsVoided = p.IsVoided,
                    VoidedOnUtc = p.VoidedOnUtc,
                    CreatedOnUtc = p.CreatedOnUtc
                })
                .ToList(),
            Events = plan.Events
                .OrderByDescending(e => e.CreatedOnUtc)
                .ThenByDescending(e => e.PayoffPlanEventId)
                .Select(e => new PayoffPlanEventDto
                {
                    PayoffPlanEventId = e.PayoffPlanEventId,
                    EventType = e.EventType,
                    Summary = e.Summary,
                    PayloadJson = e.PayloadJson,
                    CreatedOnUtc = e.CreatedOnUtc
                })
                .ToList()
        });
    }

    public async Task<ApiResponse<ActivePayoffPlanProgressDto>> GetActivePayoffPlanProgressAsync(
        CancellationToken cancellationToken = default)
    {
        var plan = await activePayoffPlanRepository.GetActiveWithDetailsAsync(cancellationToken);
        if (plan is null)
        {
            return ApiResponse<ActivePayoffPlanProgressDto>.Fail("No active payoff plan was found.");
        }

        var cards = await LoadAnalysisCardsAsync(cancellationToken);
        var progress = await BuildProgressAsync(plan, cards, cancellationToken);
        return ApiResponse<ActivePayoffPlanProgressDto>.Ok(progress);
    }

    private async Task<ApiResponse<ActivePayoffPlanDto>> EndActivePlanAsync(
        string status,
        string eventType,
        string summary,
        CancellationToken cancellationToken)
    {
        var plan = await activePayoffPlanRepository.GetActiveWithDetailsAsync(cancellationToken);
        if (plan is null)
        {
            return ApiResponse<ActivePayoffPlanDto>.Fail("No active payoff plan was found.");
        }

        var now = DateTime.UtcNow;
        plan.Status = status;
        plan.EndedOnUtc = now;
        plan.UpdatedOnUtc = now;
        await activePayoffPlanRepository.AddEventAsync(
            new PayoffPlanEvent
            {
                ActivePayoffPlanId = plan.ActivePayoffPlanId,
                EventType = eventType,
                Summary = summary,
                CreatedOnUtc = now
            },
            cancellationToken);
        await activePayoffPlanRepository.SaveChangesAsync(cancellationToken);

        var cards = await LoadAnalysisCardsAsync(cancellationToken);
        var progress = await BuildProgressAsync(plan, cards, cancellationToken);
        return ApiResponse<ActivePayoffPlanDto>.Ok(ToActivePayoffPlanDto(plan, progress));
    }

    private async Task<ApiResponse<PlanKnobs>> ResolveActivationConfigAsync(
        ActivatePayoffPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SavedPayoffPlanId is > 0)
        {
            var saved = await savedPayoffPlanRepository.GetByIdAsync(
                request.SavedPayoffPlanId.Value,
                cancellationToken);
            if (saved is null)
            {
                return ApiResponse<PlanKnobs>.Fail("Saved payoff plan was not found.");
            }

            return ApiResponse<PlanKnobs>.Ok(new PlanKnobs
            {
                Name = string.IsNullOrWhiteSpace(request.Name) ? saved.Name : request.Name.Trim(),
                SourceSavedPayoffPlanId = saved.SavedPayoffPlanId,
                Goal = saved.Goal,
                Strategy = saved.Strategy,
                ExtraMonthlyPayment = saved.ExtraMonthlyPayment,
                TotalMonthlyDebtPayment = saved.TotalMonthlyDebtPayment,
                TargetUtilizationPercent = saved.TargetUtilizationPercent,
                PayOverLimitFirst = saved.PayOverLimitFirst,
                PostUtilizationStrategy = saved.PostUtilizationStrategy,
                EnableCashAdvanceBalanceMoves = saved.EnableCashAdvanceBalanceMoves,
                LoanAmount = saved.LoanAmount,
                LoanAnnualPercentageRate = saved.LoanAnnualPercentageRate,
                LoanApplyStrategy = saved.LoanApplyStrategy,
                LoanApplyCreditCardIdsJson = saved.LoanApplyCreditCardIdsJson,
                LoanType = saved.LoanType,
                LoanTermMonths = saved.LoanTermMonths,
                LoanInterestOnlyMonths = saved.LoanInterestOnlyMonths,
                LoanFixedMonthlyPayment = saved.LoanFixedMonthlyPayment,
                PromotionalTransfersJson = saved.PromotionalTransfersJson
            });
        }

        if (string.IsNullOrWhiteSpace(request.Strategy) || request.TotalMonthlyDebtPayment is not > 0)
        {
            return ApiResponse<PlanKnobs>.Fail(
                "Provide a savedPayoffPlanId or inline strategy and totalMonthlyDebtPayment.");
        }

        var strategy = CreateForecastRequestValidator.ParseStrategy(request.Strategy);
        if (strategy is null)
        {
            return ApiResponse<PlanKnobs>.Fail(
                "Strategy must be Avalanche, Snowball, or MinimumsOnly.");
        }

        return ApiResponse<PlanKnobs>.Ok(new PlanKnobs
        {
            Name = string.IsNullOrWhiteSpace(request.Name)
                ? $"{PayoffStrategyNames.ToDisplayName(strategy.Value)} plan"
                : request.Name.Trim(),
            Goal = NormalizeGoal(request.Goal),
            Strategy = PayoffStrategyNames.ToDisplayName(strategy.Value),
            ExtraMonthlyPayment = CreditCardMath.RoundMoney(request.ExtraMonthlyPayment ?? 0m),
            TotalMonthlyDebtPayment = CreditCardMath.RoundMoney(request.TotalMonthlyDebtPayment.Value),
            TargetUtilizationPercent = request.TargetUtilizationPercent is > 0 and <= 99
                ? request.TargetUtilizationPercent
                : null,
            PayOverLimitFirst = request.PayOverLimitFirst,
            PostUtilizationStrategy = NormalizePostUtilizationStrategyLabel(request.PostUtilizationStrategy),
            EnableCashAdvanceBalanceMoves = request.EnableCashAdvanceBalanceMoves,
            LoanAmount = request.LoanAmount is > 0
                ? CreditCardMath.RoundMoney(request.LoanAmount.Value)
                : null,
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
            PromotionalTransfersJson = SerializePromotionalTransfers(request.PromotionalTransfers)
        });
    }

    private async Task<List<Account>> LoadAnalysisCardsAsync(CancellationToken cancellationToken)
    {
        var accounts = await LoadCreditCardsAsync(cancellationToken);
        return accounts
            .Where(a => !a.IsPaidOff && a.Balance > 0)
            .Where(IsIncludedInPayoffAnalysis)
            .ToList();
    }

    private async Task<ActivePayoffPlanProgressDto> BuildProgressAsync(
        ActivePayoffPlan plan,
        IReadOnlyList<Account> analysisCards,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var currentDebt = CreditCardMath.RoundMoney(analysisCards.Sum(a => a.Balance));
        var payments = plan.Payments.Where(p => !p.IsVoided).ToList();
        var paidToDate = CreditCardMath.RoundMoney(payments.Sum(p => p.Amount));
        var debtReduced = CreditCardMath.RoundMoney(Math.Max(0m, plan.StartingDebt - currentDebt));
        var inputs = analysisCards.Select(ToPayoffInput).ToList();
        var knobs = PlanKnobs.FromActive(plan);
        var projection = inputs.Count == 0
            ? new ProjectionSnapshot(0, 0m, null, true, [])
            : RunPlanProjection(knobs, inputs, DateOnly.FromDateTime(DateTime.UtcNow));

        var monthsActive = Math.Max(
            1,
            ((DateTime.UtcNow.Year - plan.StartedOnUtc.Year) * 12)
            + DateTime.UtcNow.Month
            - plan.StartedOnUtc.Month
            + 1);
        var averageMonthlyPaid = CreditCardMath.RoundMoney(paidToDate / monthsActive);
        string? adherenceNote = null;
        if (plan.TotalMonthlyDebtPayment > 0)
        {
            var ratio = averageMonthlyPaid / plan.TotalMonthlyDebtPayment;
            adherenceNote = ratio switch
            {
                >= 0.95m => "On track: average monthly payments meet or exceed the plan budget.",
                >= 0.7m => "Slightly behind: average monthly payments are under the planned budget.",
                _ when paidToDate == 0m => "No plan payments recorded yet.",
                _ => "Behind plan: average monthly payments are well under the planned budget."
            };
        }

        return new ActivePayoffPlanProgressDto
        {
            StartingDebt = plan.StartingDebt,
            CurrentDebt = currentDebt,
            PaidToDate = paidToDate,
            DebtReduced = debtReduced,
            ProjectedMonthsRemaining = projection.Months,
            ProjectedRemainingInterest = projection.Interest,
            ProjectedPayoffDate = projection.PayoffDate,
            ProjectionIsValid = projection.IsValid,
            PlannedMonthlyPayment = plan.TotalMonthlyDebtPayment,
            AverageMonthlyPaid = averageMonthlyPaid,
            AdherenceNote = adherenceNote,
            Warnings = projection.Warnings
        };
    }

    private ProjectionSnapshot RunPlanProjection(
        PlanKnobs knobs,
        IReadOnlyList<CreditCardPayoffInput> inputs,
        DateOnly startDate)
    {
        var strategy = CreateForecastRequestValidator.ParseStrategy(knobs.Strategy)
            ?? PayoffStrategyType.Avalanche;
        var targetUtilization = knobs.TargetUtilizationPercent is > 0 and <= 99
            ? knobs.TargetUtilizationPercent
            : null;
        var promoTransfers = DeserializePromotionalTransfers(knobs.PromotionalTransfersJson)
            .Where(t => t.FromCreditCardId != t.ToCreditCardId && t.PromotionalPeriodMonths > 0)
            .Select(t => new PromotionalBalanceTransferPlan(
                t.FromCreditCardId,
                t.ToCreditCardId,
                t.Amount is > 0 ? t.Amount : null,
                t.PromotionalAnnualPercentageRate,
                t.PromotionalPeriodMonths,
                Math.Max(0, t.ApplyAtMonthOffset)))
            .ToList();
        var postUtilizationStrategy = ParsePostUtilizationStrategy(knobs.PostUtilizationStrategy);
        var loan = ResolveLoanPlanArgs(
            knobs.LoanAmount,
            knobs.LoanAnnualPercentageRate,
            knobs.LoanApplyStrategy,
            DeserializeLoanApplyCreditCardIds(knobs.LoanApplyCreditCardIdsJson),
            knobs.LoanType,
            knobs.LoanTermMonths,
            knobs.LoanInterestOnlyMonths,
            knobs.LoanFixedMonthlyPayment);
        var totalPayment = CreditCardMath.RoundMoney(
            knobs.TotalMonthlyDebtPayment + loan.RequiredMonthlyBudget);

        var result = payoffEngine.GeneratePlan(new PayoffPlanRequest(
            inputs,
            totalPayment,
            strategy,
            startDate,
            targetUtilization,
            knobs.PayOverLimitFirst,
            knobs.EnableCashAdvanceBalanceMoves,
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

        return new ProjectionSnapshot(
            result.MonthsToPayoff,
            result.TotalInterestPaid,
            result.OverallDebtFreeDate,
            result.IsValid,
            result.Warnings.ToList());
    }

    private static PayoffPlanVersion CreateVersionSnapshot(
        ActivePayoffPlan plan,
        int versionNumber,
        string reason,
        decimal snapshotDebt,
        ProjectionSnapshot projection,
        DateTime createdOnUtc) =>
        new()
        {
            ActivePayoffPlanId = plan.ActivePayoffPlanId,
            VersionNumber = versionNumber,
            Reason = reason,
            CreatedOnUtc = createdOnUtc,
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
            LoanApplyCreditCardIdsJson = plan.LoanApplyCreditCardIdsJson,
            LoanType = plan.LoanType,
            LoanTermMonths = plan.LoanTermMonths,
            LoanInterestOnlyMonths = plan.LoanInterestOnlyMonths,
            LoanFixedMonthlyPayment = plan.LoanFixedMonthlyPayment,
            PromotionalTransfersJson = plan.PromotionalTransfersJson,
            SnapshotDebt = snapshotDebt,
            ProjectedMonthsToPayoff = projection.Months,
            ProjectedTotalInterest = projection.Interest,
            ProjectedPayoffDate = projection.PayoffDate,
            ProjectionIsValid = projection.IsValid
        };

    /// <summary>Decrements balance (floor 0) and updates paid-off flags. Returns amount applied.</summary>
    internal static decimal ApplyPaymentToAccount(Account account, decimal amount)
    {
        var applied = CreditCardMath.RoundMoney(Math.Min(account.Balance, amount));
        account.Balance = CreditCardMath.RoundMoney(Math.Max(0m, account.Balance - amount));
        if (account.Balance <= 0m)
        {
            account.Balance = 0m;
            account.IsPaidOff = true;
            account.PaidOffDate ??= DateTime.UtcNow.Date;
        }
        else
        {
            account.IsPaidOff = false;
            account.PaidOffDate = null;
        }

        return applied;
    }

    internal static void RestorePaymentToAccount(Account account, decimal amount)
    {
        account.Balance = CreditCardMath.RoundMoney(account.Balance + amount);
        if (account.Balance > 0m)
        {
            account.IsPaidOff = false;
            account.PaidOffDate = null;
        }
    }

    private static ActivePayoffPlanDto ToActivePayoffPlanDto(
        ActivePayoffPlan plan,
        ActivePayoffPlanProgressDto progress) =>
        new()
        {
            ActivePayoffPlanId = plan.ActivePayoffPlanId,
            Name = plan.Name,
            Status = plan.Status,
            SourceSavedPayoffPlanId = plan.SourceSavedPayoffPlanId,
            StartedOnUtc = plan.StartedOnUtc,
            EndedOnUtc = plan.EndedOnUtc,
            CurrentVersionNumber = plan.CurrentVersionNumber,
            StartingDebt = plan.StartingDebt,
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
            Progress = progress
        };

    private static PayoffPlanVersionDto ToVersionDto(PayoffPlanVersion version) =>
        new()
        {
            PayoffPlanVersionId = version.PayoffPlanVersionId,
            VersionNumber = version.VersionNumber,
            Reason = version.Reason,
            CreatedOnUtc = version.CreatedOnUtc,
            Strategy = version.Strategy,
            TotalMonthlyDebtPayment = version.TotalMonthlyDebtPayment,
            SnapshotDebt = version.SnapshotDebt,
            ProjectedMonthsToPayoff = version.ProjectedMonthsToPayoff,
            ProjectedTotalInterest = version.ProjectedTotalInterest,
            ProjectedPayoffDate = version.ProjectedPayoffDate,
            ProjectionIsValid = version.ProjectionIsValid
        };

    private sealed class PlanKnobs
    {
        public string Name { get; init; } = string.Empty;
        public int? SourceSavedPayoffPlanId { get; init; }
        public string? Goal { get; init; }
        public string Strategy { get; init; } = string.Empty;
        public decimal ExtraMonthlyPayment { get; init; }
        public decimal TotalMonthlyDebtPayment { get; init; }
        public decimal? TargetUtilizationPercent { get; init; }
        public bool PayOverLimitFirst { get; init; }
        public string? PostUtilizationStrategy { get; init; }
        public bool EnableCashAdvanceBalanceMoves { get; init; }
        public decimal? LoanAmount { get; init; }
        public decimal? LoanAnnualPercentageRate { get; init; }
        public string? LoanApplyStrategy { get; init; }
        public string? LoanApplyCreditCardIdsJson { get; init; }
        public string? LoanType { get; init; }
        public int? LoanTermMonths { get; init; }
        public int? LoanInterestOnlyMonths { get; init; }
        public decimal? LoanFixedMonthlyPayment { get; init; }
        public string? PromotionalTransfersJson { get; init; }

        public static PlanKnobs FromActive(ActivePayoffPlan plan) =>
            new()
            {
                Name = plan.Name,
                SourceSavedPayoffPlanId = plan.SourceSavedPayoffPlanId,
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
                LoanApplyCreditCardIdsJson = plan.LoanApplyCreditCardIdsJson,
                LoanType = plan.LoanType,
                LoanTermMonths = plan.LoanTermMonths,
                LoanInterestOnlyMonths = plan.LoanInterestOnlyMonths,
                LoanFixedMonthlyPayment = plan.LoanFixedMonthlyPayment,
                PromotionalTransfersJson = plan.PromotionalTransfersJson
            };
    }

    private sealed record ProjectionSnapshot(
        int Months,
        decimal Interest,
        DateOnly? PayoffDate,
        bool IsValid,
        IReadOnlyList<string> Warnings);
}
