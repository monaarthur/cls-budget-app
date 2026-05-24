using CLS.Budget.Application.Budgets.Dtos;
using CLS.Budget.Application.Common;

namespace CLS.Budget.Application.Abstractions.Services;

public interface IBudgetService
{
    Task<ApiResponse<IReadOnlyList<BudgetResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<BudgetResponse>>> GetByMonthAndYearAsync(int month, int year, CancellationToken cancellationToken = default);
    Task<ApiResponse<BudgetResponse>> GetByIdAsync(int budgetId, CancellationToken cancellationToken = default);
    Task<ApiResponse<BudgetResponse>> CreateAsync(CreateBudgetRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<BudgetResponse>> CopyAsync(int sourceBudgetId, CopyBudgetRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<BudgetResponse>> UpdateAsync(int budgetId, UpdateBudgetRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<BudgetResponse>> AddAccountAsync(
        int budgetId,
        int accountId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<BudgetResponse>> RemoveAccountAsync(
        int budgetId,
        int accountId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteAsync(int budgetId, CancellationToken cancellationToken = default);
}
