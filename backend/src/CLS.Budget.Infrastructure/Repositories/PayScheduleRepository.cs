using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class PayFrequencyTypeRepository(BudgetDbContext dbContext) : IPayFrequencyTypeRepository
{
    public async Task<IReadOnlyList<PayFrequencyType>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.PayFrequencyTypes
            .AsNoTracking()
            .OrderBy(t => t.PayFrequencyTypeId)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsAsync(int payFrequencyTypeId, CancellationToken cancellationToken = default) =>
        await dbContext.PayFrequencyTypes
            .AsNoTracking()
            .AnyAsync(t => t.PayFrequencyTypeId == payFrequencyTypeId, cancellationToken);
}

public sealed class IncomeSourceRepository(BudgetDbContext dbContext) : IIncomeSourceRepository
{
    public async Task<IReadOnlyList<IncomeSource>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.IncomeSources
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task<IncomeSource?> GetByIdAsync(
        int incomeSourceId,
        CancellationToken cancellationToken = default) =>
        await dbContext.IncomeSources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.IncomeSourceId == incomeSourceId, cancellationToken);

    public async Task<bool> ExistsAsync(int incomeSourceId, CancellationToken cancellationToken = default) =>
        await dbContext.IncomeSources
            .AsNoTracking()
            .AnyAsync(s => s.IncomeSourceId == incomeSourceId, cancellationToken);

    public async Task<IncomeSource?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default) =>
        await dbContext.IncomeSources
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Name.ToLower() == name.ToLower(),
                cancellationToken);

    public async Task<IncomeSource> AddAsync(
        IncomeSource source,
        CancellationToken cancellationToken = default)
    {
        dbContext.IncomeSources.Add(source);
        await dbContext.SaveChangesAsync(cancellationToken);
        return source;
    }
}

public sealed class PayScheduleRepository(BudgetDbContext dbContext) : IPayScheduleRepository
{
    public async Task<IReadOnlyList<PaySchedule>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.PaySchedules
            .AsNoTracking()
            .Include(s => s.IncomeSource)
            .Include(s => s.PayFrequencyType)
            .OrderByDescending(s => s.IsDefault)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task<PaySchedule?> GetByIdAsync(
        int payScheduleId,
        CancellationToken cancellationToken = default) =>
        await dbContext.PaySchedules
            .Include(s => s.IncomeSource)
            .Include(s => s.PayFrequencyType)
            .FirstOrDefaultAsync(s => s.PayScheduleId == payScheduleId, cancellationToken);

    public async Task<PaySchedule?> GetDefaultAsync(CancellationToken cancellationToken = default) =>
        await dbContext.PaySchedules
            .Include(s => s.IncomeSource)
            .Include(s => s.PayFrequencyType)
            .FirstOrDefaultAsync(s => s.IsDefault && s.IsActive, cancellationToken);

    public async Task<PaySchedule> AddAsync(PaySchedule schedule, CancellationToken cancellationToken = default)
    {
        dbContext.PaySchedules.Add(schedule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return schedule;
    }

    public async Task UpdateAsync(PaySchedule schedule, CancellationToken cancellationToken = default)
    {
        dbContext.PaySchedules.Update(schedule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearDefaultFlagAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.PaySchedules
            .Where(s => s.IsDefault)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.IsDefault, false),
                cancellationToken);
    }
}
