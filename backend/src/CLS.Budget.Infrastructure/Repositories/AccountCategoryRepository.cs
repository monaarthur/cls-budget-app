using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class AccountCategoryRepository(BudgetDbContext dbContext) : IAccountCategoryRepository
{
    public async Task<IReadOnlyList<AccountCategory>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.AccountCategories
            .AsNoTracking()
            .OrderBy(c => c.AccountCategoryId)
            .ToListAsync(cancellationToken);
}
