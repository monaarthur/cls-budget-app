using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface IActivePayoffPlanRepository
{
    Task<ActivePayoffPlan?> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<ActivePayoffPlan?> GetActiveWithDetailsAsync(CancellationToken cancellationToken = default);

    Task<ActivePayoffPlan?> GetByIdWithDetailsAsync(
        int activePayoffPlanId,
        CancellationToken cancellationToken = default);

    Task<PayoffPlanPayment?> GetPaymentAsync(
        int payoffPlanPaymentId,
        CancellationToken cancellationToken = default);

    Task AddAsync(ActivePayoffPlan plan, CancellationToken cancellationToken = default);

    Task AddVersionAsync(PayoffPlanVersion version, CancellationToken cancellationToken = default);

    Task AddPaymentAsync(PayoffPlanPayment payment, CancellationToken cancellationToken = default);

    Task AddEventAsync(PayoffPlanEvent planEvent, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
