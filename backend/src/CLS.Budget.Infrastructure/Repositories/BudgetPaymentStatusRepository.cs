using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class BudgetPaymentStatusRepository(BudgetDbContext dbContext) : IBudgetPaymentStatusRepository
{
    public async Task<IReadOnlyList<BudgetPaymentStatus>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.BudgetPaymentStatuses
            .AsNoTracking()
            .OrderBy(s => s.BudgetPaymentStatusId)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(int budgetPaymentStatusId, CancellationToken cancellationToken = default) =>
        dbContext.BudgetPaymentStatuses.AnyAsync(s => s.BudgetPaymentStatusId == budgetPaymentStatusId, cancellationToken);
}
