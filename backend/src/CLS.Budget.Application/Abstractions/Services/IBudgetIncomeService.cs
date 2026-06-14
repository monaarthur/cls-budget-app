using CLS.Budget.Application.Common;
using CLS.Budget.Application.Incomes.Dtos;

namespace CLS.Budget.Application.Abstractions.Services;

public interface IBudgetIncomeService
{
    Task<ApiResponse<IReadOnlyList<IncomeResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<IncomeResponse>> GetByIdAsync(int incomeId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<IncomeResponse>>> GetByBudgetIdAsync(
        int budgetId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<IncomeResponse>> CreateAsync(
        CreateIncomeRequest request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<IncomeResponse>> UpdateAsync(
        int incomeId,
        UpdateIncomeRequest request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteAsync(int incomeId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IncomeSummaryResponse>> GetSummaryByBudgetIdAsync(
        int budgetId,
        CancellationToken cancellationToken = default);
}
