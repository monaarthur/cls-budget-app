using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface IAccountRepository
{
    Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Account>> GetByCategoryAsync(int accountCategoryId, CancellationToken cancellationToken = default);
    Task<Account?> GetByIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Account>> GetByIdsAsync(IReadOnlyList<int> accountIds, CancellationToken cancellationToken = default);
    Task<Account?> GetByIdAndCategoryAsync(int accountId, int accountCategoryId, CancellationToken cancellationToken = default);
    Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default);
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
    Task DeleteAsync(Account account, CancellationToken cancellationToken = default);
}
