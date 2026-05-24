using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface IBudgetRepository
{
    Task<IReadOnlyList<BudgetModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BudgetModel>> GetByMonthAndYearAsync(int month, int year, CancellationToken cancellationToken = default);
    Task<BudgetModel?> GetByIdAsync(int budgetId, CancellationToken cancellationToken = default);
    Task<BudgetModel?> GetByIdWithPaymentsAsync(int budgetId, CancellationToken cancellationToken = default);
    Task<BudgetModel?> GetByMonthYearWithPaymentsAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default);
    Task<BudgetModel> AddAsync(BudgetModel budget, CancellationToken cancellationToken = default);
    Task<BudgetModel> AddWithPaymentsForAccountsAsync(
        BudgetModel budget,
        IReadOnlyList<Account> accounts,
        CancellationToken cancellationToken = default);
    Task<BudgetModel> CopyWithPaymentsAsync(
        BudgetModel source,
        BudgetModel newBudget,
        CancellationToken cancellationToken = default);
    Task UpdateAsync(BudgetModel budget, CancellationToken cancellationToken = default);
    Task AddAccountWithPaymentAsync(
        BudgetModel budget,
        Account account,
        CancellationToken cancellationToken = default);
    Task RemoveAccountWithPaymentAsync(
        BudgetModel budget,
        int accountId,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(BudgetModel budget, CancellationToken cancellationToken = default);
}
