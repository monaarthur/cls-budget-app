namespace CLS.Budget.Import.Accounts;

/// <summary>
/// Row shape for PersonalMonthlyBills export (Save As CSV from Excel).
/// </summary>
internal sealed class MonthlyBillsCsvRow
{
    public string? Active { get; set; }
    public string? Category { get; set; }
    public string? AccountBill { get; set; }
    public string? AccountNumber { get; set; }
    public string? DueDate { get; set; }
    public string? DueDay { get; set; }
    public string? CurrentBalanceOwe { get; set; }
    public string? MonthlyPayment { get; set; }
    public string? PaidWith { get; set; }
    public string? MonthlyDueOverride { get; set; }
    public string? PaymentDateExtra { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public string? Interest { get; set; }
    public string? LastUpdate { get; set; }
    public string? AvailableCredit { get; set; }
    public string? CreditLimit { get; set; }
    public string? Url { get; set; }
}
