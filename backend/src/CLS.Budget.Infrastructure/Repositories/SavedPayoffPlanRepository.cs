using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class SavedPayoffPlanRepository(BudgetDbContext dbContext) : ISavedPayoffPlanRepository
{
    public async Task<IReadOnlyList<SavedPayoffPlan>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.SavedPayoffPlans
            .OrderByDescending(p => p.CreatedOnUtc)
            .ToListAsync(cancellationToken);

    public async Task<SavedPayoffPlan?> GetByIdAsync(
        int savedPayoffPlanId,
        CancellationToken cancellationToken = default) =>
        await dbContext.SavedPayoffPlans
            .FirstOrDefaultAsync(p => p.SavedPayoffPlanId == savedPayoffPlanId, cancellationToken);

    public async Task<IReadOnlyList<SavedPayoffPlan>> GetByIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await dbContext.SavedPayoffPlans
            .Where(p => ids.Contains(p.SavedPayoffPlanId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SavedPayoffPlan plan, CancellationToken cancellationToken = default) =>
        await dbContext.SavedPayoffPlans.AddAsync(plan, cancellationToken);

    public Task DeleteAsync(SavedPayoffPlan plan, CancellationToken cancellationToken = default)
    {
        dbContext.SavedPayoffPlans.Remove(plan);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
