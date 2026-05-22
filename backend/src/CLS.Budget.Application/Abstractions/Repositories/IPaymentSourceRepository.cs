using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface IPaymentSourceRepository
{
    Task<IReadOnlyList<PaymentSource>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaymentSource?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<PaymentSource> AddAsync(PaymentSource paymentSource, CancellationToken cancellationToken = default);
}
