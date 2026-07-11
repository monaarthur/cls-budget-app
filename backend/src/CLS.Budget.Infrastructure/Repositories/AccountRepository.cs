using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class AccountRepository(BudgetDbContext dbContext) : IAccountRepository
{
    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Accounts
            .AsNoTracking()
            .Include(a => a.CreditCardDetail)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Account>> GetByCategoryAsync(
        int accountCategoryId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Accounts
            .AsNoTracking()
            .Include(a => a.CreditCardDetail)
            .Where(a => a.AccountCategoryId == accountCategoryId)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

    public async Task<Account?> GetByIdAsync(int accountId, CancellationToken cancellationToken = default) =>
        await dbContext.Accounts
            .Include(a => a.CreditCardDetail)
            .FirstOrDefaultAsync(a => a.AccountId == accountId, cancellationToken);

    public async Task<IReadOnlyList<Account>> GetByIdsAsync(
        IReadOnlyList<int> accountIds,
        CancellationToken cancellationToken = default)
    {
        if (accountIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Accounts
            .AsNoTracking()
            .Include(a => a.CreditCardDetail)
            .Where(a => accountIds.Contains(a.AccountId))
            .ToListAsync(cancellationToken);
    }

    public async Task<Account?> GetByIdAndCategoryAsync(
        int accountId,
        int accountCategoryId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Accounts
            .Include(a => a.CreditCardDetail)
            .FirstOrDefaultAsync(
                a => a.AccountId == accountId && a.AccountCategoryId == accountCategoryId,
                cancellationToken);

    public async Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        dbContext.Accounts.Update(account);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Account account, CancellationToken cancellationToken = default)
    {
        dbContext.Accounts.Remove(account);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
