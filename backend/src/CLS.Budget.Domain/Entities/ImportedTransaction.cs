namespace CLS.Budget.Domain.Entities;

/// <summary>
/// A single staged transaction line from an uploaded CSV.
/// Description is free text (e.g. merchant name) and does not require a matching account.
/// </summary>
public class ImportedTransaction : ITenantOwned
{
    public int ImportedTransactionId { get; set; }
    public Guid TenantId { get; set; }
    public int TransactionImportId { get; set; }
    public TransactionImport? TransactionImport { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = null!;
    public string? CategoryRaw { get; set; }
    public int? AccountCategoryId { get; set; }
    public AccountCategory? AccountCategory { get; set; }
    public decimal Amount { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string? PostingStatusRaw { get; set; }
    public int BudgetPaymentStatusId { get; set; }
    public BudgetPaymentStatus? BudgetPaymentStatus { get; set; }
    public int? IncomeSourceId { get; set; }
    public IncomeSource? IncomeSource { get; set; }
    public string? Notes { get; set; }
}
