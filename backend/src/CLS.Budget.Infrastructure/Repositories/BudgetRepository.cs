using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Budgets;
using CLS.Budget.Domain;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class BudgetRepository(BudgetDbContext dbContext) : IBudgetRepository
{
    public async Task<IReadOnlyList<BudgetModel>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Budgets
            .AsNoTracking()
            .OrderByDescending(b => b.StartPeriod)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BudgetModel>> GetByMonthAndYearAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default) =>
        await dbContext.Budgets
            .AsNoTracking()
            .Where(b => b.StartPeriod.Month == month && b.StartPeriod.Year == year)
            .OrderBy(b => b.StartPeriod)
            .ToListAsync(cancellationToken);

    public async Task<BudgetModel?> GetByIdAsync(int budgetId, CancellationToken cancellationToken = default) =>
        await dbContext.Budgets.FirstOrDefaultAsync(b => b.BudgetId == budgetId, cancellationToken);

    public async Task<BudgetModel?> GetByIdWithPaymentsAsync(
        int budgetId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Budgets
            .Include(b => b.BudgetPayments)
            .FirstOrDefaultAsync(b => b.BudgetId == budgetId, cancellationToken);

    public async Task<BudgetModel?> GetByMonthYearWithPaymentsAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default) =>
        await dbContext.Budgets
            .Include(b => b.BudgetPayments)
            .FirstOrDefaultAsync(
                b => b.StartPeriod.Month == month && b.StartPeriod.Year == year,
                cancellationToken);

    public async Task<BudgetModel> AddAsync(BudgetModel budget, CancellationToken cancellationToken = default)
    {
        dbContext.Budgets.Add(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
        return budget;
    }

    public async Task<BudgetModel> AddWithPaymentsForAccountsAsync(
        BudgetModel budget,
        IReadOnlyList<Account> accounts,
        CancellationToken cancellationToken = default)
    {
        dbContext.Budgets.Add(budget);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var account in accounts)
        {
            dbContext.BudgetPayments.Add(new BudgetPayment
            {
                BudgetId = budget.BudgetId,
                AccountId = account.AccountId,
                PaymentMade = account.MonthlyPayment ?? 0m,
                Amount = account.MonthlyPayment ?? 0m,
                PaymentDate = budget.StartPeriod,
                BudgetPaymentStatusId = BudgetPaymentStatusIds.Scheduled,
                IsCleared = false
            });
        }

        if (accounts.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return budget;
    }

    public async Task<BudgetModel> CopyWithPaymentsAsync(
        BudgetModel source,
        BudgetModel newBudget,
        CancellationToken cancellationToken = default)
    {
        dbContext.Budgets.Add(newBudget);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var payment in source.BudgetPayments)
        {
            dbContext.BudgetPayments.Add(new BudgetPayment
            {
                BudgetId = newBudget.BudgetId,
                AccountId = payment.AccountId,
                PaymentMade = payment.PaymentMade,
                Amount = payment.Amount,
                BudgetPaymentStatusId = BudgetPaymentStatusIds.Scheduled,
                IsCleared = false,
                PaymentDate = BudgetPaymentDateMapper.MapToBudgetPeriod(
                    payment.PaymentDate,
                    newBudget.StartPeriod),
                ClearedDate = null,
                PaymentSourceId = payment.PaymentSourceId
            });
        }

        if (source.BudgetPayments.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return newBudget;
    }

    public async Task UpdateAsync(BudgetModel budget, CancellationToken cancellationToken = default)
    {
        dbContext.Budgets.Update(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAccountWithPaymentAsync(
        BudgetModel budget,
        Account account,
        CancellationToken cancellationToken = default)
    {
        dbContext.Budgets.Update(budget);

        var existingPayment = await dbContext.BudgetPayments
            .FirstOrDefaultAsync(
                p => p.BudgetId == budget.BudgetId && p.AccountId == account.AccountId,
                cancellationToken);

        if (existingPayment is null)
        {
            dbContext.BudgetPayments.Add(new BudgetPayment
            {
                BudgetId = budget.BudgetId,
                AccountId = account.AccountId,
                PaymentMade = account.MonthlyPayment ?? 0m,
                Amount = account.MonthlyPayment ?? 0m,
                PaymentDate = budget.StartPeriod,
                BudgetPaymentStatusId = BudgetPaymentStatusIds.Scheduled,
                IsCleared = false
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAccountWithPaymentAsync(
        BudgetModel budget,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        dbContext.Budgets.Update(budget);

        var payments = await dbContext.BudgetPayments
            .Where(p => p.BudgetId == budget.BudgetId && p.AccountId == accountId)
            .ToListAsync(cancellationToken);

        if (payments.Count > 0)
        {
            dbContext.BudgetPayments.RemoveRange(payments);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(BudgetModel budget, CancellationToken cancellationToken = default)
    {
        dbContext.Budgets.Remove(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
