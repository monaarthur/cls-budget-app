using CLS.Budget.Application.Abstractions;
using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class AccountCategoryRepository(
    BudgetDbContext dbContext,
    ITenantContext tenantContext) : IAccountCategoryRepository
{
    public async Task<IReadOnlyList<AccountCategory>> GetAllForTenantAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        return await dbContext.AccountCategories
            .AsNoTracking()
            .Include(c => c.SubCategories)
            .Where(c => c.TenantId == null || c.TenantId == tenantId)
            .OrderBy(c => c.IsSystem ? 0 : 1)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountCategory?> GetByIdForTenantAsync(
        int accountCategoryId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        return await dbContext.AccountCategories
            .AsNoTracking()
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(
                c => c.AccountCategoryId == accountCategoryId
                     && (c.TenantId == null || c.TenantId == tenantId),
                cancellationToken);
    }

    public async Task<AccountCategory?> FindByNameForTenantAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        var normalized = name.Trim().ToLowerInvariant();
        return await dbContext.AccountCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => (c.TenantId == null || c.TenantId == tenantId)
                     && c.Name.ToLower() == normalized,
                cancellationToken);
    }

    public async Task<AccountCategory> AddCategoryAsync(
        AccountCategory category,
        CancellationToken cancellationToken = default)
    {
        category.TenantId = tenantContext.TenantId;
        category.IsSystem = false;
        dbContext.AccountCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<AccountSubCategory?> GetSubCategoryByIdAsync(
        int accountSubCategoryId,
        CancellationToken cancellationToken = default) =>
        await dbContext.AccountSubCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.AccountSubCategoryId == accountSubCategoryId,
                cancellationToken);

    public async Task<AccountSubCategory?> FindSubCategoryByNameAsync(
        int accountCategoryId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return await dbContext.AccountSubCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.AccountCategoryId == accountCategoryId
                     && s.Name.ToLower() == normalized,
                cancellationToken);
    }

    public async Task<AccountSubCategory> AddSubCategoryAsync(
        AccountSubCategory subCategory,
        CancellationToken cancellationToken = default)
    {
        dbContext.AccountSubCategories.Add(subCategory);
        await dbContext.SaveChangesAsync(cancellationToken);
        return subCategory;
    }
}
