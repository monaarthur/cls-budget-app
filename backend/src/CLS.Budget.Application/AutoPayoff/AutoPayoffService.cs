using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.AutoPayoff.Dtos;
using CLS.Budget.Application.Common;
using CLS.Budget.Domain.AutoPayoff;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.AutoPayoff;

public sealed class AutoPayoffService(
    IAutoPayoffConfigRepository configRepository,
    IAccountRepository accountRepository,
    IPaymentRepository paymentRepository,
    AutoPayoffEngine engine) : IAutoPayoffService
{
    public async Task<ApiResponse<IReadOnlyList<AutoPayoffConfigResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var configs = await configRepository.GetAllAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<AutoPayoffConfigResponse>>.Ok(
            configs.Select(AutoPayoffMapper.ToResponse).ToList());
    }

    public async Task<ApiResponse<AutoPayoffConfigResponse>> GetConfigAsync(
        CancellationToken cancellationToken = default)
    {
        var entity = await configRepository.GetAsync(cancellationToken);
        var data = entity is null
            ? AutoPayoffMapper.DefaultConfig()
            : AutoPayoffMapper.ToResponse(entity);
        return ApiResponse<AutoPayoffConfigResponse>.Ok(data);
    }

    public async Task<ApiResponse<AutoPayoffConfigResponse>> GetConfigAsync(
        int autoPayoffConfigId,
        CancellationToken cancellationToken = default)
    {
        var entity = await configRepository.GetByIdAsync(autoPayoffConfigId, cancellationToken);
        if (entity is null)
        {
            return ApiResponse<AutoPayoffConfigResponse>.Fail("Auto Budget was not found.");
        }

        return ApiResponse<AutoPayoffConfigResponse>.Ok(AutoPayoffMapper.ToResponse(entity));
    }

    public async Task<ApiResponse<AutoPayoffConfigResponse>> SaveConfigAsync(
        SaveAutoPayoffConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = AutoPayoffMapper.NormalizeName(request.Name);
        var now = DateTime.UtcNow;
        var existingId = request.AutoPayoffConfigId is > 0
            ? request.AutoPayoffConfigId
            : null;

        if (await configRepository.NameExistsAsync(name, existingId, cancellationToken))
        {
            return ApiResponse<AutoPayoffConfigResponse>.Fail(
                $"An Auto Budget named \"{name}\" already exists.");
        }

        if (existingId is int id)
        {
            var existing = await configRepository.GetByIdAsync(id, cancellationToken);
            if (existing is null)
            {
                return ApiResponse<AutoPayoffConfigResponse>.Fail("Auto Budget was not found.");
            }

            AutoPayoffMapper.Apply(existing, request, now);
            await configRepository.UpdateAsync(existing, cancellationToken);
            var reloaded = await configRepository.GetByIdAsync(id, cancellationToken) ?? existing;
            return ApiResponse<AutoPayoffConfigResponse>.Ok(AutoPayoffMapper.ToResponse(reloaded));
        }

        var created = new AutoPayoffConfig { CreatedOnUtc = now };
        AutoPayoffMapper.Apply(created, request, now);
        created = await configRepository.AddAsync(created, cancellationToken);
        return ApiResponse<AutoPayoffConfigResponse>.Ok(AutoPayoffMapper.ToResponse(created));
    }

    public async Task<ApiResponse<AutoPayoffPreviewResponse>> PreviewAsync(
        SaveAutoPayoffConfigRequest? request,
        CancellationToken cancellationToken = default)
    {
        AutoPayoffConfigResponse config;
        if (request is null)
        {
            var saved = await GetConfigAsync(cancellationToken);
            config = saved.Data ?? AutoPayoffMapper.DefaultConfig();
        }
        else
        {
            config = new AutoPayoffConfigResponse
            {
                AutoPayoffConfigId = request.AutoPayoffConfigId,
                Name = AutoPayoffMapper.NormalizeName(request.Name),
                ExcludeStatusIds = request.ExcludeStatusIds,
                ExcludeCategoryIds = request.ExcludeCategoryIds,
                ExcludeAccountIds = request.ExcludeAccountIds,
                TargetCategoryIds = request.TargetCategoryIds,
                TargetByPayoffDate = request.TargetByPayoffDate,
                ExtraMonthlyAmount = Math.Max(0, request.ExtraMonthlyAmount),
            };
        }

        var accounts = await accountRepository.GetAllAsync(cancellationToken);
        var payments = await paymentRepository.GetAllAsync(cancellationToken);
        var latestPaymentByAccount = payments
            .GroupBy(p => p.AccountId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(p => p.PaymentDate).First());

        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var inputs = accounts.Select(account => ToInput(account, latestPaymentByAccount)).ToList();
        var plan = engine.BuildPlan(inputs, AutoPayoffMapper.ToRules(config), config.ExtraMonthlyAmount, asOf);

        return ApiResponse<AutoPayoffPreviewResponse>.Ok(new AutoPayoffPreviewResponse
        {
            Config = config,
            Queue = plan.Queue.Select(item => new AutoPayoffPreviewItemResponse
            {
                Rank = item.Rank,
                AccountId = item.AccountId,
                Name = item.Name,
                CategoryId = item.CategoryId,
                CategoryName = item.CategoryName,
                Balance = item.Balance,
                MonthlyPayment = item.MonthlyPayment,
                TargetDate = item.TargetDate?.ToDateTime(TimeOnly.MinValue),
                Reason = item.Reason,
                ExtraAllocated = item.ExtraAllocated,
            }).ToList(),
            Excluded = plan.Excluded.Select(item => new AutoPayoffExcludedItemResponse
            {
                AccountId = item.AccountId,
                Name = item.Name,
                Reason = item.Reason,
            }).ToList(),
            ExtraPayment = plan.ExtraPayment,
            ExtraAllocated = plan.ExtraAllocated,
            ExtraRemaining = plan.ExtraRemaining,
        });
    }

    private static AutoPayoffAccountInput ToInput(
        Account account,
        IReadOnlyDictionary<int, BudgetPayment> payments)
    {
        payments.TryGetValue(account.AccountId, out var payment);
        DateOnly? nextPaymentDate = payment is null
            ? null
            : DateOnly.FromDateTime(DateTime.SpecifyKind(payment.PaymentDate, DateTimeKind.Utc));

        return new AutoPayoffAccountInput(
            account.AccountId,
            account.Name,
            account.AccountCategoryId,
            account.AccountCategory?.Name ?? $"Category {account.AccountCategoryId}",
            account.Balance,
            account.MonthlyPayment,
            account.PaymentDay,
            nextPaymentDate,
            payment?.BudgetPaymentStatusId,
            payment?.BudgetPaymentStatus?.Name,
            account.IsPaidOff,
            account.CreditCardDetail?.IncludeInPayoffAnalysis ?? true);
    }
}
