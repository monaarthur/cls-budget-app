using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Budgets.Dtos;
using CLS.Budget.Application.Common;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Budgets;

public sealed class BudgetService(
    IBudgetRepository budgetRepository,
    IBudgetTemplateRepository budgetTemplateRepository,
    IAccountRepository accountRepository) : IBudgetService
{
    public async Task<ApiResponse<IReadOnlyList<BudgetResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var budgets = await budgetRepository.GetAllAsync(cancellationToken);
        var data = budgets.Select(BudgetMapper.ToResponse).ToList();
        return ApiResponse<IReadOnlyList<BudgetResponse>>.Ok(data);
    }

    public async Task<ApiResponse<IReadOnlyList<BudgetResponse>>> GetByMonthAndYearAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        if (month is < 1 or > 12)
        {
            return ApiResponse<IReadOnlyList<BudgetResponse>>.Fail("Month must be between 1 and 12.");
        }

        var budgets = await budgetRepository.GetByMonthAndYearAsync(month, year, cancellationToken);
        var data = budgets.Select(BudgetMapper.ToResponse).ToList();
        return ApiResponse<IReadOnlyList<BudgetResponse>>.Ok(data);
    }

    public async Task<ApiResponse<BudgetResponse>> GetByIdAsync(
        int budgetId,
        CancellationToken cancellationToken = default)
    {
        var budget = await budgetRepository.GetByIdAsync(budgetId, cancellationToken);
        if (budget is null)
        {
            return ApiResponse<BudgetResponse>.Fail($"Budget with id {budgetId} was not found.");
        }

        return ApiResponse<BudgetResponse>.Ok(BudgetMapper.ToResponse(budget));
    }

    public async Task<ApiResponse<BudgetResponse>> CreateAsync(
        CreateBudgetRequest request,
        CancellationToken cancellationToken = default)
    {
        var template = await budgetTemplateRepository.GetByIdAsync(request.BudgetTemplateId, cancellationToken);
        if (template is null)
        {
            return ApiResponse<BudgetResponse>.Fail(
                $"Budget template with id {request.BudgetTemplateId} was not found.");
        }

        var accountIds = BudgetTemplateAccountIdsParser.Parse(template.AccountIds);
        IReadOnlyList<Account> orderedAccounts;
        string serializedAccountIds;

        if (accountIds.Count == 0)
        {
            var allAccounts = await accountRepository.GetAllAsync(cancellationToken);
            if (allAccounts.Count == 0)
            {
                return ApiResponse<BudgetResponse>.Fail(
                    "No accounts exist to include in a budget.");
            }

            orderedAccounts = allAccounts.OrderBy(a => a.Name).ToList();
            serializedAccountIds = BudgetTemplateAccountIdsParser.Serialize(
                orderedAccounts.Select(a => a.AccountId).ToList());
        }
        else
        {
            var accounts = await accountRepository.GetByIdsAsync(accountIds, cancellationToken);
            var foundIds = accounts.Select(a => a.AccountId).ToHashSet();
            var missingIds = accountIds.Where(id => !foundIds.Contains(id)).ToList();
            if (missingIds.Count > 0)
            {
                return ApiResponse<BudgetResponse>.Fail(
                    $"Accounts not found for template: {string.Join(", ", missingIds)}.");
            }

            var accountsById = accounts.ToDictionary(a => a.AccountId);
            orderedAccounts = accountIds
                .Where(id => accountsById.ContainsKey(id))
                .Select(id => accountsById[id])
                .ToList();
            serializedAccountIds = template.AccountIds!;
        }

        var budget = BudgetMapper.ToEntity(request);
        budget.AccountIds = serializedAccountIds;
        var created = await budgetRepository.AddWithPaymentsForAccountsAsync(
            budget,
            orderedAccounts,
            cancellationToken);

        return ApiResponse<BudgetResponse>.Ok(BudgetMapper.ToResponse(created));
    }

    public async Task<ApiResponse<BudgetResponse>> CopyAsync(
        int sourceBudgetId,
        CopyBudgetRequest request,
        CancellationToken cancellationToken = default)
    {
        var source = await budgetRepository.GetByIdWithPaymentsAsync(sourceBudgetId, cancellationToken);
        if (source is null)
        {
            return ApiResponse<BudgetResponse>.Fail($"Budget with id {sourceBudgetId} was not found.");
        }

        var budgetTemplateId = request.BudgetTemplateId ?? source.BudgetTemplateId;
        if (request.BudgetTemplateId.HasValue)
        {
            var template = await budgetTemplateRepository.GetByIdAsync(budgetTemplateId, cancellationToken);
            if (template is null)
            {
                return ApiResponse<BudgetResponse>.Fail(
                    $"Budget template with id {budgetTemplateId} was not found.");
            }
        }

        var accountIds = BudgetTemplateAccountIdsParser.Parse(source.AccountIds);
        if (accountIds.Count == 0)
        {
            return ApiResponse<BudgetResponse>.Fail(
                "Source budget has no AccountIds configured.");
        }

        var accounts = await accountRepository.GetByIdsAsync(accountIds, cancellationToken);
        var foundIds = accounts.Select(a => a.AccountId).ToHashSet();
        var missingIds = accountIds.Where(id => !foundIds.Contains(id)).ToList();
        if (missingIds.Count > 0)
        {
            return ApiResponse<BudgetResponse>.Fail(
                $"Accounts not found for source budget: {string.Join(", ", missingIds)}.");
        }

        var newBudget = BudgetMapper.ToCopiedEntity(source, request, budgetTemplateId);
        var created = await budgetRepository.CopyWithPaymentsAsync(source, newBudget, cancellationToken);

        return ApiResponse<BudgetResponse>.Ok(BudgetMapper.ToResponse(created));
    }

    public async Task<ApiResponse<BudgetResponse>> UpdateAsync(
        int budgetId,
        UpdateBudgetRequest request,
        CancellationToken cancellationToken = default)
    {
        var budget = await budgetRepository.GetByIdAsync(budgetId, cancellationToken);
        if (budget is null)
        {
            return ApiResponse<BudgetResponse>.Fail($"Budget with id {budgetId} was not found.");
        }

        BudgetMapper.ApplyUpdate(budget, request);

        if (request.AccountIds is not null)
        {
            var accountIds = request.AccountIds.ToList();
            if (accountIds.Count == 0)
            {
                return ApiResponse<BudgetResponse>.Fail("AccountIds cannot be empty.");
            }

            var accounts = await accountRepository.GetByIdsAsync(accountIds, cancellationToken);
            var foundIds = accounts.Select(a => a.AccountId).ToHashSet();
            var missingIds = accountIds.Where(id => !foundIds.Contains(id)).ToList();
            if (missingIds.Count > 0)
            {
                return ApiResponse<BudgetResponse>.Fail(
                    $"Accounts not found: {string.Join(", ", missingIds)}.");
            }

            budget.AccountIds = BudgetTemplateAccountIdsParser.Serialize(accountIds);
        }

        await budgetRepository.UpdateAsync(budget, cancellationToken);
        return ApiResponse<BudgetResponse>.Ok(BudgetMapper.ToResponse(budget));
    }

    public async Task<ApiResponse<object>> DeleteAsync(
        int budgetId,
        CancellationToken cancellationToken = default)
    {
        var budget = await budgetRepository.GetByIdAsync(budgetId, cancellationToken);
        if (budget is null)
        {
            return ApiResponse<object>.Fail($"Budget with id {budgetId} was not found.");
        }

        await budgetRepository.DeleteAsync(budget, cancellationToken);
        return ApiResponse<object>.Ok(new { });
    }
}
