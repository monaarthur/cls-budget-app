using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface IForecastScenarioRepository
{
    Task<ForecastScenario?> GetByIdAsync(int forecastScenarioId, CancellationToken cancellationToken = default);
    Task AddAsync(ForecastScenario scenario, CancellationToken cancellationToken = default);
    Task DeleteAsync(ForecastScenario scenario, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
