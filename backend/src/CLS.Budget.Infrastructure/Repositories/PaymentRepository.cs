using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class PaymentRepository(BudgetDbContext dbContext) : IPaymentRepository
{
    private IQueryable<BudgetPayment> PaymentsWithStatus =>
        dbContext.BudgetPayments
            .Include(p => p.BudgetPaymentStatus)
            .Include(p => p.IncomeSource);

    public async Task<IReadOnlyList<BudgetPayment>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await PaymentsWithStatus
            .AsNoTracking()
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

    public async Task<BudgetPayment?> GetByIdAsync(int paymentId, CancellationToken cancellationToken = default) =>
        await PaymentsWithStatus
            .FirstOrDefaultAsync(p => p.BudgetPaymentId == paymentId, cancellationToken);

    public async Task<BudgetPayment?> GetByBudgetAndAccountAsync(
        int budgetId,
        int accountId,
        CancellationToken cancellationToken = default) =>
        await dbContext.BudgetPayments
            .FirstOrDefaultAsync(
                p => p.BudgetId == budgetId && p.AccountId == accountId,
                cancellationToken);

    public async Task<IReadOnlyList<BudgetPayment>> GetByBudgetIdAsync(
        int budgetId,
        CancellationToken cancellationToken = default) =>
        await PaymentsWithStatus
            .AsNoTracking()
            .Where(p => p.BudgetId == budgetId)
            .OrderBy(p => p.AccountId)
            .ToListAsync(cancellationToken);

    public async Task<BudgetPayment> AddAsync(BudgetPayment payment, CancellationToken cancellationToken = default)
    {
        dbContext.BudgetPayments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(payment.BudgetPaymentId, cancellationToken))!;
    }

    public async Task UpdateAsync(BudgetPayment payment, CancellationToken cancellationToken = default)
    {
        dbContext.BudgetPayments.Update(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(BudgetPayment payment, CancellationToken cancellationToken = default)
    {
        dbContext.BudgetPayments.Remove(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
