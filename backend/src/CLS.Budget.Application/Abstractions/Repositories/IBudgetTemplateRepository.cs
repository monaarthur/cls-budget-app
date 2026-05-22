using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface IBudgetTemplateRepository
{
    Task<IReadOnlyList<BudgetTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BudgetTemplate?> GetByIdAsync(int budgetTemplateId, CancellationToken cancellationToken = default);
}
