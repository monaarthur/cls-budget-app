using CLS.Budget.Application.Common;
using CLS.Budget.Application.PaymentSources.Dtos;

namespace CLS.Budget.Application.Abstractions.Services;

public interface IPaymentSourceService
{
    Task<ApiResponse<IReadOnlyList<PaymentSourceResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
