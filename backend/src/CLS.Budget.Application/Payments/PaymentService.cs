using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Common;
using CLS.Budget.Application.Payments.Dtos;

namespace CLS.Budget.Application.Payments;

public sealed class PaymentService(
    IPaymentRepository paymentRepository,
    IBudgetPaymentStatusRepository budgetPaymentStatusRepository) : IPaymentService
{
    public async Task<ApiResponse<IReadOnlyList<PaymentResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var payments = await paymentRepository.GetAllAsync(cancellationToken);
        var data = payments.Select(PaymentMapper.ToResponse).ToList();
        return ApiResponse<IReadOnlyList<PaymentResponse>>.Ok(data);
    }

    public async Task<ApiResponse<PaymentResponse>> GetByIdAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            return ApiResponse<PaymentResponse>.Fail($"Payment with id {paymentId} was not found.");
        }

        return ApiResponse<PaymentResponse>.Ok(PaymentMapper.ToResponse(payment));
    }

    public async Task<ApiResponse<PaymentResponse>> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var statusError = await ValidateBudgetPaymentStatusAsync(request.BudgetPaymentStatusId, cancellationToken);
        if (statusError is not null)
        {
            return ApiResponse<PaymentResponse>.Fail(statusError);
        }

        var payment = PaymentMapper.ToEntity(request);
        var created = await paymentRepository.AddAsync(payment, cancellationToken);
        return ApiResponse<PaymentResponse>.Ok(PaymentMapper.ToResponse(created));
    }

    public async Task<ApiResponse<PaymentResponse>> UpdateAsync(
        int paymentId,
        UpdatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var payment = await paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            return ApiResponse<PaymentResponse>.Fail($"Payment with id {paymentId} was not found.");
        }

        var statusError = await ValidateBudgetPaymentStatusAsync(request.BudgetPaymentStatusId, cancellationToken);
        if (statusError is not null)
        {
            return ApiResponse<PaymentResponse>.Fail(statusError);
        }

        PaymentMapper.ApplyUpdate(payment, request);
        await paymentRepository.UpdateAsync(payment, cancellationToken);
        var updated = await paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        return ApiResponse<PaymentResponse>.Ok(PaymentMapper.ToResponse(updated!));
    }

    public async Task<ApiResponse<object>> DeleteAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            return ApiResponse<object>.Fail($"Payment with id {paymentId} was not found.");
        }

        await paymentRepository.DeleteAsync(payment, cancellationToken);
        return ApiResponse<object>.Ok(new { });
    }

    private async Task<string?> ValidateBudgetPaymentStatusAsync(
        int budgetPaymentStatusId,
        CancellationToken cancellationToken)
    {
        if (budgetPaymentStatusId <= 0)
        {
            return "BudgetPaymentStatusId must be greater than 0.";
        }

        var exists = await budgetPaymentStatusRepository.ExistsAsync(budgetPaymentStatusId, cancellationToken);
        return exists ? null : $"Budget payment status with id {budgetPaymentStatusId} was not found.";
    }
}
