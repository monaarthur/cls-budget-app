namespace CLS.Budget.Application.CreditCardEngine.Dtos;

public sealed class LoanScheduleRequestDto
{
    /// <summary>Personal, HomeEquity, Heloc, Retirement401k, Family.</summary>
    public string LoanType { get; init; } = null!;
    public decimal Amount { get; init; }
    public decimal AnnualPercentageRate { get; init; }
    public int? TermMonths { get; init; }
    public int? InterestOnlyMonths { get; init; }
    public decimal? FixedMonthlyPayment { get; init; }
}

public sealed class LoanScheduleMonthDto
{
    public int MonthNumber { get; init; }
    public decimal Payment { get; init; }
    public decimal Interest { get; init; }
    public decimal Principal { get; init; }
    public decimal EndingBalance { get; init; }
}

public sealed class LoanScheduleResultDto
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public string LoanType { get; init; } = null!;
    public string LoanTypeDisplayName { get; init; } = null!;
    public decimal MonthlyPayment { get; init; }
    public decimal? Phase2MonthlyPayment { get; init; }
    public int MonthsToPayoff { get; init; }
    public decimal TotalInterest { get; init; }
    public decimal TotalPaid { get; init; }
    public IReadOnlyList<LoanScheduleMonthDto> Schedule { get; init; } = [];
}
