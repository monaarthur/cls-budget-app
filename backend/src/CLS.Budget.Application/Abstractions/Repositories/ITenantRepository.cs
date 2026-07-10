using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken cancellationToken = default);
    Task<Tenant> AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
}
