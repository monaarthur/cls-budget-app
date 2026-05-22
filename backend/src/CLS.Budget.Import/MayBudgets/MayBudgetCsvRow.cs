namespace CLS.Budget.Import.MayBudgets;

internal sealed class MayBudgetCsvRow
{
    public string? Active { get; set; }
    public string? Month { get; set; }
    public string? AccountBill { get; set; }
    public string? Category { get; set; }
    public string? DueDay { get; set; }
    public string? ApproxAmount { get; set; }
    public string? MonthlyDueOverride { get; set; }
    public string? AmountToPay { get; set; }
    public string? AmountLeft { get; set; }
    public string? DatePaid { get; set; }
    public string? PaymentAccount { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}
