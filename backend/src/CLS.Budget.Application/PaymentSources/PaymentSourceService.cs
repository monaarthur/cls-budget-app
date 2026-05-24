using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Common;
using CLS.Budget.Application.PaymentSources.Dtos;

namespace CLS.Budget.Application.PaymentSources;

public sealed class PaymentSourceService(IPaymentSourceRepository paymentSourceRepository)
    : IPaymentSourceService
{
    public async Task<ApiResponse<IReadOnlyList<PaymentSourceResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var sources = await paymentSourceRepository.GetAllAsync(cancellationToken);
        var data = sources.Select(PaymentSourceMapper.ToResponse).ToList();
        return ApiResponse<IReadOnlyList<PaymentSourceResponse>>.Ok(data);
    }
}
