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
        PaymentDate = ToUtcDate(payment.PaymentDate),
        ClearedDate = ToUtcDate(payment.ClearedDate),
        PaymentSourceId = payment.PaymentSourceId,
        IncomeSourceId = payment.IncomeSourceId,
        IncomeSourceName = payment.IncomeSource?.Name
    };

    public static BudgetPayment ToEntity(CreatePaymentRequest request) => new()
    {
        BudgetId = request.BudgetId,
        AccountId = request.AccountId,
        PaymentMade = request.PaymentMade,
        Amount = request.Amount,
        BudgetPaymentStatusId = request.BudgetPaymentStatusId,
        IsCleared = request.IsCleared,
        PaymentDate = ToUtcDate(request.PaymentDate),
        ClearedDate = ToUtcDate(request.ClearedDate),
        PaymentSourceId = request.PaymentSourceId,
        IncomeSourceId = request.IncomeSourceId
    };

    public static void ApplyUpdate(BudgetPayment payment, UpdatePaymentRequest request)
    {
        payment.BudgetId = request.BudgetId;
        payment.AccountId = request.AccountId;
        payment.PaymentMade = request.PaymentMade;
        payment.Amount = request.Amount;
        payment.BudgetPaymentStatusId = request.BudgetPaymentStatusId;
        payment.IsCleared = request.IsCleared;
        payment.PaymentDate = ToUtcDate(request.PaymentDate);
        payment.ClearedDate = ToUtcDate(request.ClearedDate);
        payment.PaymentSourceId = request.PaymentSourceId;
        payment.IncomeSourceId = request.IncomeSourceId;
    }

    private static DateTime ToUtcDate(DateTime value) =>
        DateTime.SpecifyKind(value.ToUniversalTime().Date, DateTimeKind.Utc);

    private static DateTime? ToUtcDate(DateTime? value) =>
        value.HasValue ? ToUtcDate(value.Value) : null;
}
