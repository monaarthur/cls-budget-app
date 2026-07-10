using System.Text.Json.Serialization;
using CLS.Budget.Application.Common.Serialization;

namespace CLS.Budget.Application.Payments.Dtos;

public sealed class PaymentResponse
{
    public int BudgetPaymentId { get; init; }
    public int BudgetId { get; init; }
    public int AccountId { get; init; }
    public decimal PaymentMade { get; init; }
    public decimal Amount { get; init; }
    public int BudgetPaymentStatusId { get; init; }
    public string BudgetPaymentStatusName { get; init; } = null!;
    public bool IsCleared { get; init; }

    [JsonConverter(typeof(DateOnlyUtcJsonConverter))]
    public DateTime PaymentDate { get; init; }

    [JsonConverter(typeof(NullableDateOnlyUtcJsonConverter))]
    public DateTime? ClearedDate { get; init; }
    public int? PaymentSourceId { get; init; }
    public int? IncomeSourceId { get; init; }
    public string? IncomeSourceName { get; init; }
}
