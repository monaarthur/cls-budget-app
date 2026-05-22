using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface IBudgetPaymentStatusRepository
{
    Task<IReadOnlyList<BudgetPaymentStatus>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int budgetPaymentStatusId, CancellationToken cancellationToken = default);
}
