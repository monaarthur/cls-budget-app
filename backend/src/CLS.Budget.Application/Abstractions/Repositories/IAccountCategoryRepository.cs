using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface IAccountCategoryRepository
{
    Task<IReadOnlyList<AccountCategory>> GetAllForTenantAsync(
        CancellationToken cancellationToken = default);

    Task<AccountCategory?> GetByIdForTenantAsync(
        int accountCategoryId,
        CancellationToken cancellationToken = default);

    Task<AccountCategory?> FindByNameForTenantAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<AccountCategory> AddCategoryAsync(
        AccountCategory category,
        CancellationToken cancellationToken = default);

    Task<AccountSubCategory?> GetSubCategoryByIdAsync(
        int accountSubCategoryId,
        CancellationToken cancellationToken = default);

    Task<AccountSubCategory?> FindSubCategoryByNameAsync(
        int accountCategoryId,
        string name,
        CancellationToken cancellationToken = default);

    Task<AccountSubCategory> AddSubCategoryAsync(
        AccountSubCategory subCategory,
        CancellationToken cancellationToken = default);
}
