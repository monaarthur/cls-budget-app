using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface IPaymentRepository
{
    Task<IReadOnlyList<BudgetPayment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BudgetPayment?> GetByIdAsync(int paymentId, CancellationToken cancellationToken = default);
    Task<BudgetPayment?> GetByBudgetAndAccountAsync(
        int budgetId,
        int accountId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BudgetPayment>> GetByBudgetIdAsync(
        int budgetId,
        CancellationToken cancellationToken = default);
    Task<BudgetPayment> AddAsync(BudgetPayment payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(BudgetPayment payment, CancellationToken cancellationToken = default);
    Task DeleteAsync(BudgetPayment payment, CancellationToken cancellationToken = default);
}
