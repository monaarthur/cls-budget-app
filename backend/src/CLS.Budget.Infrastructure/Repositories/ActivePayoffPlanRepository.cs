using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class ActivePayoffPlanRepository(BudgetDbContext dbContext) : IActivePayoffPlanRepository
{
    public async Task<ActivePayoffPlan?> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ActivePayoffPlans
            .FirstOrDefaultAsync(p => p.Status == ActivePayoffPlanStatuses.Active, cancellationToken);

    public async Task<ActivePayoffPlan?> GetActiveWithDetailsAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.ActivePayoffPlans
            .Include(p => p.Versions)
            .Include(p => p.Payments)
            .Include(p => p.Events)
            .FirstOrDefaultAsync(p => p.Status == ActivePayoffPlanStatuses.Active, cancellationToken);

    public async Task<ActivePayoffPlan?> GetByIdWithDetailsAsync(
        int activePayoffPlanId,
        CancellationToken cancellationToken = default) =>
        await dbContext.ActivePayoffPlans
            .Include(p => p.Versions)
            .Include(p => p.Payments)
            .Include(p => p.Events)
            .FirstOrDefaultAsync(p => p.ActivePayoffPlanId == activePayoffPlanId, cancellationToken);

    public async Task<PayoffPlanPayment?> GetPaymentAsync(
        int payoffPlanPaymentId,
        CancellationToken cancellationToken = default) =>
        await dbContext.PayoffPlanPayments
            .FirstOrDefaultAsync(p => p.PayoffPlanPaymentId == payoffPlanPaymentId, cancellationToken);

    public async Task AddAsync(ActivePayoffPlan plan, CancellationToken cancellationToken = default) =>
        await dbContext.ActivePayoffPlans.AddAsync(plan, cancellationToken);

    public async Task AddVersionAsync(
        PayoffPlanVersion version,
        CancellationToken cancellationToken = default) =>
        await dbContext.PayoffPlanVersions.AddAsync(version, cancellationToken);

    public async Task AddPaymentAsync(
        PayoffPlanPayment payment,
        CancellationToken cancellationToken = default) =>
        await dbContext.PayoffPlanPayments.AddAsync(payment, cancellationToken);

    public async Task AddEventAsync(
        PayoffPlanEvent planEvent,
        CancellationToken cancellationToken = default) =>
        await dbContext.PayoffPlanEvents.AddAsync(planEvent, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
