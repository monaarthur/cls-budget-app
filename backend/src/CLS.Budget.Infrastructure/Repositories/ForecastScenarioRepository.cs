using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class ForecastScenarioRepository(BudgetDbContext dbContext) : IForecastScenarioRepository
{
    public async Task<ForecastScenario?> GetByIdAsync(
        int forecastScenarioId,
        CancellationToken cancellationToken = default) =>
        await dbContext.ForecastScenarios
            .Include(s => s.CreditCards)
            .Include(s => s.MonthlySnapshots)
            .FirstOrDefaultAsync(s => s.ForecastScenarioId == forecastScenarioId, cancellationToken);

    public async Task AddAsync(ForecastScenario scenario, CancellationToken cancellationToken = default) =>
        await dbContext.ForecastScenarios.AddAsync(scenario, cancellationToken);

    public Task DeleteAsync(ForecastScenario scenario, CancellationToken cancellationToken = default)
    {
        dbContext.ForecastScenarios.Remove(scenario);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
