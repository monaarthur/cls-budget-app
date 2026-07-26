namespace CLS.Budget.Domain.CreditCardEngine.Loan;

public enum LoanType
{
    Personal = 1,
    HomeEquity = 2,
    Heloc = 3,
    Retirement401k = 4,
    Family = 5
}

public sealed record LoanScheduleRequest(
    LoanType LoanType,
    decimal Amount,
    decimal AnnualPercentageRate,
    int? TermMonths = null,
    int? InterestOnlyMonths = null,
    decimal? FixedMonthlyPayment = null);

public sealed record LoanScheduleMonth(
    int MonthNumber,
    decimal Payment,
    decimal Interest,
    decimal Principal,
    decimal EndingBalance);

public sealed record LoanScheduleResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    string LoanTypeDisplayName,
    decimal MonthlyPayment,
    decimal? Phase2MonthlyPayment,
    int MonthsToPayoff,
    decimal TotalInterest,
    decimal TotalPaid,
    IReadOnlyList<LoanScheduleMonth> Schedule);
