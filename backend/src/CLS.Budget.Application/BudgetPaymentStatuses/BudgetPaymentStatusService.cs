using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.BudgetPaymentStatuses.Dtos;
using CLS.Budget.Application.Common;

namespace CLS.Budget.Application.BudgetPaymentStatuses;

public sealed class BudgetPaymentStatusService(IBudgetPaymentStatusRepository budgetPaymentStatusRepository)
    : IBudgetPaymentStatusService
{
    public async Task<ApiResponse<IReadOnlyList<BudgetPaymentStatusResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var statuses = await budgetPaymentStatusRepository.GetAllAsync(cancellationToken);
        var data = statuses.Select(BudgetPaymentStatusMapper.ToResponse).ToList();
        return ApiResponse<IReadOnlyList<BudgetPaymentStatusResponse>>.Ok(data);
    }
}
