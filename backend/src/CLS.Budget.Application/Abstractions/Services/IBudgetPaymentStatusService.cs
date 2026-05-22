using CLS.Budget.Application.BudgetPaymentStatuses.Dtos;
using CLS.Budget.Application.Common;

namespace CLS.Budget.Application.Abstractions.Services;

public interface IBudgetPaymentStatusService
{
    Task<ApiResponse<IReadOnlyList<BudgetPaymentStatusResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
