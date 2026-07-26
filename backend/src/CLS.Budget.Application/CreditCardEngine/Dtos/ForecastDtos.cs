using System.Text.Json.Serialization;
using CLS.Budget.Application.Common.Serialization;

namespace CLS.Budget.Application.CreditCardEngine.Dtos;

public sealed class CreateForecastRequest
{
    public string Strategy { get; init; } = "Avalanche";
    public decimal TotalMonthlyDebtPayment { get; init; }
    public int ForecastMonths { get; init; } = 120;

    [JsonConverter(typeof(NullableDateOnlyUtcJsonConverter))]
    public DateTime? StartDate { get; init; }

    public decimal? MonthlyNetIncome { get; init; }
    public decimal? MonthlyExpenses { get; init; }
    public decimal? TargetUtilizationPercent { get; init; }
    public bool PayOverLimitFirst { get; init; }

    /// <summary>When set, the forecast is persisted and an id is returned.</summary>
    public string? Name { get; init; }

    public bool Save { get; init; }

    public IReadOnlyList<ForecastChargeDto>? AdditionalCharges { get; init; }
    public IReadOnlyList<ForecastOneTimePaymentDto>? OneTimePayments { get; init; }
    public IReadOnlyList<ForecastPaymentOverrideDto>? PaymentOverrides { get; init; }
    public IReadOnlyList<ForecastIncomeChangeDto>? IncomeChanges { get; init; }
    public IReadOnlyList<ForecastExpenseChangeDto>? ExpenseChanges { get; init; }
}

public sealed class ForecastChargeDto
{
    public int MonthOffset { get; init; }
    public decimal Amount { get; init; }
    public int? CreditCardId { get; init; }
}

public sealed class ForecastOneTimePaymentDto
{
    public int MonthOffset { get; init; }
    public decimal Amount { get; init; }
}

public sealed class ForecastPaymentOverrideDto
{
    public int MonthOffset { get; init; }
    public decimal TotalMonthlyDebtPayment { get; init; }
}

public sealed class ForecastIncomeChangeDto
{
    public int MonthOffset { get; init; }
    public decimal MonthlyNetIncomeDelta { get; init; }
}

public sealed class ForecastExpenseChangeDto
{
    public int MonthOffset { get; init; }
    public decimal MonthlyExpenseDelta { get; init; }
}

public sealed class ForecastMonthDto
{
    public DateOnly Month { get; init; }
    public int MonthIndex { get; init; }
    public decimal StartingDebt { get; init; }
    public decimal NewCharges { get; init; }
    public decimal Interest { get; init; }
    public decimal Payments { get; init; }
    public decimal EndingDebt { get; init; }
    public decimal TotalCreditLimit { get; init; }
    public decimal OverallUtilizationPercentage { get; init; }
    public decimal AvailableCash { get; init; }
    public int CardsPaidOffThisMonth { get; init; }
    public decimal CumulativeInterest { get; init; }
}

public sealed class ForecastResultDto
{
    public int? ForecastId { get; init; }
    public string? Name { get; init; }
    public string Strategy { get; init; } = null!;
    public decimal StartingDebt { get; init; }
    public decimal MonthlyPayment { get; init; }
    public int ForecastMonths { get; init; }
    public DateOnly? EstimatedDebtFreeDate { get; init; }
    public decimal TotalInterestPaid { get; init; }
    public IReadOnlyList<ForecastMonthDto> Months { get; init; } = [];
}
