using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface ISavedPayoffPlanRepository
{
    Task<IReadOnlyList<SavedPayoffPlan>> ListAsync(CancellationToken cancellationToken = default);
    Task<SavedPayoffPlan?> GetByIdAsync(int savedPayoffPlanId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavedPayoffPlan>> GetByIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default);
    Task AddAsync(SavedPayoffPlan plan, CancellationToken cancellationToken = default);
    Task DeleteAsync(SavedPayoffPlan plan, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
