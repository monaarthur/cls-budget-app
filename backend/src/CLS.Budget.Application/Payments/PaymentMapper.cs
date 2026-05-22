using CLS.Budget.Application.Payments.Dtos;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Payments;

internal static class PaymentMapper
{
    public static PaymentResponse ToResponse(BudgetPayment payment) => new()
    {
        BudgetPaymentId = payment.BudgetPaymentId,
        BudgetId = payment.BudgetId,
        AccountId = payment.AccountId,
        PaymentMade = payment.PaymentMade,
        Amount = payment.Amount,
        BudgetPaymentStatusId = payment.BudgetPaymentStatusId,
        BudgetPaymentStatusName = payment.BudgetPaymentStatus?.Name ?? string.Empty,
        IsCleared = payment.IsCleared,
        PaymentDate = payment.PaymentDate,
        ClearedDate = payment.ClearedDate,
        PaymentSourceId = payment.PaymentSourceId
    };

    public static BudgetPayment ToEntity(CreatePaymentRequest request) => new()
    {
        BudgetId = request.BudgetId,
        AccountId = request.AccountId,
        PaymentMade = request.PaymentMade,
        Amount = request.Amount,
        BudgetPaymentStatusId = request.BudgetPaymentStatusId,
        IsCleared = request.IsCleared,
        PaymentDate = request.PaymentDate,
        ClearedDate = request.ClearedDate,
        PaymentSourceId = request.PaymentSourceId
    };

    public static void ApplyUpdate(BudgetPayment payment, UpdatePaymentRequest request)
    {
        payment.BudgetId = request.BudgetId;
        payment.AccountId = request.AccountId;
        payment.PaymentMade = request.PaymentMade;
        payment.Amount = request.Amount;
        payment.BudgetPaymentStatusId = request.BudgetPaymentStatusId;
        payment.IsCleared = request.IsCleared;
        payment.PaymentDate = request.PaymentDate;
        payment.ClearedDate = request.ClearedDate;
        payment.PaymentSourceId = request.PaymentSourceId;
    }
}
