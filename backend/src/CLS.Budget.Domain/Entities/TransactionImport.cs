namespace CLS.Budget.Domain.Entities;

/// <summary>
/// A CSV upload batch of bank/export transactions staged before applying to a budget.
/// </summary>
public class TransactionImport : ITenantOwned
{
    public int TransactionImportId { get; set; }
    public Guid TenantId { get; set; }
    public string FileName { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
    public int RowCount { get; set; }
    public int? IncomeSourceId { get; set; }
    public IncomeSource? IncomeSource { get; set; }
    public ICollection<ImportedTransaction> Transactions { get; set; } = [];
}
