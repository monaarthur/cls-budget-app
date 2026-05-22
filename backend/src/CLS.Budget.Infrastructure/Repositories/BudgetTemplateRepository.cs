using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class BudgetTemplateRepository(BudgetDbContext dbContext) : IBudgetTemplateRepository
{
    public async Task<IReadOnlyList<BudgetTemplate>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.BudgetTemplates
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

    public Task<BudgetTemplate?> GetByIdAsync(int budgetTemplateId, CancellationToken cancellationToken = default) =>
        dbContext.BudgetTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.BudgetTemplateId == budgetTemplateId, cancellationToken);
}
