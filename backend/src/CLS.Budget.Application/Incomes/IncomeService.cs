using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Common;
using CLS.Budget.Application.Incomes.Dtos;

namespace CLS.Budget.Application.Incomes;

public sealed class IncomeService(
    IBudgetIncomeRepository incomeRepository,
    IBudgetRepository budgetRepository,
    IIncomeSourceRepository incomeSourceRepository) : IBudgetIncomeService
{
    public async Task<ApiResponse<IReadOnlyList<IncomeResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var incomes = await incomeRepository.GetAllAsync(cancellationToken);
        var data = incomes.Select(IncomeMapper.ToResponse).ToList();
        return ApiResponse<IReadOnlyList<IncomeResponse>>.Ok(data);
    }

    public async Task<ApiResponse<IncomeResponse>> GetByIdAsync(
        int incomeId,
        CancellationToken cancellationToken = default)
    {
        var income = await incomeRepository.GetByIdAsync(incomeId, cancellationToken);
        if (income is null)
        {
            return ApiResponse<IncomeResponse>.Fail($"Income with id {incomeId} was not found.");
        }

        return ApiResponse<IncomeResponse>.Ok(IncomeMapper.ToResponse(income));
    }

    public async Task<ApiResponse<IReadOnlyList<IncomeResponse>>> GetByBudgetIdAsync(
        int budgetId,
        CancellationToken cancellationToken = default)
    {
        var incomes = await incomeRepository.GetByBudgetIdAsync(budgetId, cancellationToken);
        var data = incomes.Select(IncomeMapper.ToResponse).ToList();
        return ApiResponse<IReadOnlyList<IncomeResponse>>.Ok(data);
    }

    public async Task<ApiResponse<IncomeResponse>> CreateAsync(
        CreateIncomeRequest request,
        CancellationToken cancellationToken = default)
    {
        var budgetError = await ValidateBudgetAsync(request.BudgetId, cancellationToken);
        if (budgetError is not null)
        {
            return ApiResponse<IncomeResponse>.Fail(budgetError);
        }

        var sourceError = await ValidateIncomeSourceAsync(request.IncomeSourceId, cancellationToken);
        if (sourceError is not null)
        {
            return ApiResponse<IncomeResponse>.Fail(sourceError);
        }

        var income = IncomeMapper.ToEntity(request);
        var created = await incomeRepository.AddAsync(income, cancellationToken);
        return ApiResponse<IncomeResponse>.Ok(IncomeMapper.ToResponse(created));
    }

    public async Task<ApiResponse<IncomeResponse>> UpdateAsync(
        int incomeId,
        UpdateIncomeRequest request,
        CancellationToken cancellationToken = default)
    {
        var income = await incomeRepository.GetByIdAsync(incomeId, cancellationToken);
        if (income is null)
        {
            return ApiResponse<IncomeResponse>.Fail($"Income with id {incomeId} was not found.");
        }

        var budgetError = await ValidateBudgetAsync(request.BudgetId, cancellationToken);
        if (budgetError is not null)
        {
            return ApiResponse<IncomeResponse>.Fail(budgetError);
        }

        var sourceError = await ValidateIncomeSourceAsync(request.IncomeSourceId, cancellationToken);
        if (sourceError is not null)
        {
            return ApiResponse<IncomeResponse>.Fail(sourceError);
        }

        IncomeMapper.ApplyUpdate(income, request);
        await incomeRepository.UpdateAsync(income, cancellationToken);
        var updated = await incomeRepository.GetByIdAsync(incomeId, cancellationToken);
        return ApiResponse<IncomeResponse>.Ok(IncomeMapper.ToResponse(updated!));
    }

    public async Task<ApiResponse<object>> DeleteAsync(
        int incomeId,
        CancellationToken cancellationToken = default)
    {
        var income = await incomeRepository.GetByIdAsync(incomeId, cancellationToken);
        if (income is null)
        {
            return ApiResponse<object>.Fail($"Income with id {incomeId} was not found.");
        }

        await incomeRepository.DeleteAsync(income, cancellationToken);
        return ApiResponse<object>.Ok(new { });
    }

    public async Task<ApiResponse<IncomeSummaryResponse>> GetSummaryByBudgetIdAsync(
        int budgetId,
        CancellationToken cancellationToken = default)
    {
        var budgetError = await ValidateBudgetAsync(budgetId, cancellationToken);
        if (budgetError is not null)
        {
            return ApiResponse<IncomeSummaryResponse>.Fail(budgetError);
        }

        var incomes = await incomeRepository.GetByBudgetIdAsync(budgetId, cancellationToken);
        var items = incomes
            .GroupBy(i => new { i.IncomeSourceId, Name = i.IncomeSource?.Name ?? string.Empty })
            .Select(g => new IncomeSummaryItem
            {
                IncomeSourceId = g.Key.IncomeSourceId,
                IncomeSourceName = g.Key.Name,
                Total = g.Sum(x => x.Amount)
            })
            .OrderBy(x => x.IncomeSourceId)
            .ToList();

        var summary = new IncomeSummaryResponse
        {
            BudgetId = budgetId,
            Total = items.Sum(x => x.Total),
            Items = items
        };

        return ApiResponse<IncomeSummaryResponse>.Ok(summary);
    }

    private async Task<string?> ValidateBudgetAsync(int budgetId, CancellationToken cancellationToken)
    {
        if (budgetId <= 0)
        {
            return "BudgetId must be greater than 0.";
        }

        var budget = await budgetRepository.GetByIdAsync(budgetId, cancellationToken);
        return budget is null ? $"Budget with id {budgetId} was not found." : null;
    }

    private async Task<string?> ValidateIncomeSourceAsync(int incomeSourceId, CancellationToken cancellationToken)
    {
        if (incomeSourceId <= 0)
        {
            return "IncomeSourceId must be greater than 0.";
        }

        var exists = await incomeSourceRepository.ExistsAsync(incomeSourceId, cancellationToken);
        return exists ? null : $"Income source with id {incomeSourceId} was not found.";
    }
}
