using CLS.Budget.Application.TransactionImports.Dtos;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.TransactionImports;

internal static class TransactionImportMapper
{
    public static TransactionImportSummaryResponse ToSummaryResponse(TransactionImport import) => new()
    {
        TransactionImportId = import.TransactionImportId,
        FileName = import.FileName,
        UploadedAt = import.UploadedAt,
        RowCount = import.RowCount,
        IncomeSourceId = import.IncomeSourceId,
        IncomeSourceName = import.IncomeSource?.Name
    };

    public static TransactionImportDetailResponse ToDetailResponse(TransactionImport import) => new()
    {
        TransactionImportId = import.TransactionImportId,
        FileName = import.FileName,
        UploadedAt = import.UploadedAt,
        RowCount = import.RowCount,
        IncomeSourceId = import.IncomeSourceId,
        IncomeSourceName = import.IncomeSource?.Name,
        Transactions = import.Transactions
            .OrderBy(t => t.LineNumber)
            .Select(ToTransactionResponse)
            .ToList()
    };

    public static ImportedTransactionResponse ToTransactionResponse(ImportedTransaction transaction) => new()
    {
        ImportedTransactionId = transaction.ImportedTransactionId,
        LineNumber = transaction.LineNumber,
        Description = transaction.Description,
        CategoryRaw = transaction.CategoryRaw,
        AccountCategoryId = transaction.AccountCategoryId,
        AccountCategoryName = transaction.AccountCategory?.Name,
        Amount = transaction.Amount,
        TransactionDate = transaction.TransactionDate,
        PostingStatusRaw = transaction.PostingStatusRaw,
        BudgetPaymentStatusId = transaction.BudgetPaymentStatusId,
        BudgetPaymentStatusName = transaction.BudgetPaymentStatus?.Name ?? string.Empty,
        IncomeSourceId = transaction.IncomeSourceId,
        IncomeSourceName = transaction.IncomeSource?.Name,
        Notes = transaction.Notes
    };
}
