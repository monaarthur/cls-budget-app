using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface IBudgetIncomeRepository
{
    Task<IReadOnlyList<BudgetIncome>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BudgetIncome?> GetByIdAsync(int budgetIncomeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BudgetIncome>> GetByBudgetIdAsync(
        int budgetId,
        CancellationToken cancellationToken = default);
    Task<BudgetIncome> AddAsync(BudgetIncome income, CancellationToken cancellationToken = default);
    Task UpdateAsync(BudgetIncome income, CancellationToken cancellationToken = default);
    Task DeleteAsync(BudgetIncome income, CancellationToken cancellationToken = default);
}
