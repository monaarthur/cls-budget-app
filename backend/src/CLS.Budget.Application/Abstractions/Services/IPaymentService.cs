using CLS.Budget.Application.Common;
using CLS.Budget.Application.Payments.Dtos;

namespace CLS.Budget.Application.Abstractions.Services;

public interface IPaymentService
{
    Task<ApiResponse<IReadOnlyList<PaymentResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<PaymentResponse>> GetByIdAsync(int paymentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PaymentResponse>> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PaymentResponse>> UpdateAsync(int paymentId, UpdatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteAsync(int paymentId, CancellationToken cancellationToken = default);
}
