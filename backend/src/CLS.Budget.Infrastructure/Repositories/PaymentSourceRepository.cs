using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class PaymentSourceRepository(BudgetDbContext dbContext) : IPaymentSourceRepository
{
    public async Task<IReadOnlyList<PaymentSource>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.PaymentSources
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task<PaymentSource?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        await dbContext.PaymentSources
            .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower(), cancellationToken);

    public async Task<PaymentSource> AddAsync(PaymentSource paymentSource, CancellationToken cancellationToken = default)
    {
        dbContext.PaymentSources.Add(paymentSource);
        await dbContext.SaveChangesAsync(cancellationToken);
        return paymentSource;
    }
}
