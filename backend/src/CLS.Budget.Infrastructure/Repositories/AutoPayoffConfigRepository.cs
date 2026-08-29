using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class AutoPayoffConfigRepository(BudgetDbContext dbContext) : IAutoPayoffConfigRepository
{
    public async Task<IReadOnlyList<AutoPayoffConfig>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.AutoPayoffConfigs
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ThenBy(x => x.AutoPayoffConfigId)
            .ToListAsync(cancellationToken);

    public async Task<AutoPayoffConfig?> GetAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AutoPayoffConfigs
            .OrderByDescending(x => x.UpdatedOnUtc)
            .ThenByDescending(x => x.AutoPayoffConfigId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<AutoPayoffConfig?> GetByIdAsync(
        int autoPayoffConfigId,
        CancellationToken cancellationToken = default) =>
        await dbContext.AutoPayoffConfigs
            .FirstOrDefaultAsync(
                x => x.AutoPayoffConfigId == autoPayoffConfigId,
                cancellationToken);

    public async Task<bool> NameExistsAsync(
        string name,
        int? excludeAutoPayoffConfigId,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        var query = dbContext.AutoPayoffConfigs.Where(x => x.Name.ToLower() == normalized);
        if (excludeAutoPayoffConfigId is int id)
        {
            query = query.Where(x => x.AutoPayoffConfigId != id);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<AutoPayoffConfig> AddAsync(
        AutoPayoffConfig config,
        CancellationToken cancellationToken = default)
    {
        dbContext.AutoPayoffConfigs.Add(config);
        await dbContext.SaveChangesAsync(cancellationToken);
        return config;
    }

    public async Task UpdateAsync(AutoPayoffConfig config, CancellationToken cancellationToken = default)
    {
        dbContext.AutoPayoffConfigs.Update(config);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
