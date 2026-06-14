using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class BudgetIncomeRepository(BudgetDbContext dbContext) : IBudgetIncomeRepository
{
    private IQueryable<BudgetIncome> IncomesWithSource =>
        dbContext.BudgetIncomes.Include(i => i.IncomeSource);

    public async Task<IReadOnlyList<BudgetIncome>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await IncomesWithSource
            .AsNoTracking()
            .OrderByDescending(i => i.ReceivedDate)
            .ToListAsync(cancellationToken);

    public async Task<BudgetIncome?> GetByIdAsync(int budgetIncomeId, CancellationToken cancellationToken = default) =>
        await IncomesWithSource
            .FirstOrDefaultAsync(i => i.BudgetIncomeId == budgetIncomeId, cancellationToken);

    public async Task<IReadOnlyList<BudgetIncome>> GetByBudgetIdAsync(
        int budgetId,
        CancellationToken cancellationToken = default) =>
        await IncomesWithSource
            .AsNoTracking()
            .Where(i => i.BudgetId == budgetId)
            .OrderBy(i => i.IncomeSourceId)
            .ThenBy(i => i.ReceivedDate)
            .ToListAsync(cancellationToken);

    public async Task<BudgetIncome> AddAsync(BudgetIncome income, CancellationToken cancellationToken = default)
    {
        dbContext.BudgetIncomes.Add(income);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(income.BudgetIncomeId, cancellationToken))!;
    }

    public async Task UpdateAsync(BudgetIncome income, CancellationToken cancellationToken = default)
    {
        dbContext.BudgetIncomes.Update(income);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(BudgetIncome income, CancellationToken cancellationToken = default)
    {
        dbContext.BudgetIncomes.Remove(income);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
