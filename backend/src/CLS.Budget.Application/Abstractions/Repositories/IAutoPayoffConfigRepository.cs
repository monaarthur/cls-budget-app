using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface IAutoPayoffConfigRepository
{
    Task<IReadOnlyList<AutoPayoffConfig>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AutoPayoffConfig?> GetAsync(CancellationToken cancellationToken = default);
    Task<AutoPayoffConfig?> GetByIdAsync(int autoPayoffConfigId, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(
        string name,
        int? excludeAutoPayoffConfigId,
        CancellationToken cancellationToken = default);
    Task<AutoPayoffConfig> AddAsync(AutoPayoffConfig config, CancellationToken cancellationToken = default);
    Task UpdateAsync(AutoPayoffConfig config, CancellationToken cancellationToken = default);
}
