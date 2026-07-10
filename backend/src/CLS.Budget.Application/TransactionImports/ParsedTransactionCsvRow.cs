namespace CLS.Budget.Application.TransactionImports;

internal sealed class ParsedTransactionCsvRow
{
    public int LineNumber { get; init; }
    public string Description { get; init; } = null!;
    public string? Category { get; init; }
    public decimal Amount { get; init; }
    public DateTime? TransactionDate { get; init; }
    public string? PostingStatus { get; init; }
    public string? Notes { get; init; }
}
