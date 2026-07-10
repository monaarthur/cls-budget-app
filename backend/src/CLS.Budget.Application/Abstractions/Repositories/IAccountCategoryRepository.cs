using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface IAccountCategoryRepository
{
    Task<IReadOnlyList<AccountCategory>> GetAllAsync(CancellationToken cancellationToken = default);
}
