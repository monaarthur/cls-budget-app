using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Common;
using CLS.Budget.Application.TransactionImports.Dtos;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.TransactionImports;

public sealed class TransactionImportService(
    ITransactionImportRepository transactionImportRepository,
    IAccountCategoryRepository accountCategoryRepository,
    IIncomeSourceRepository incomeSourceRepository) : ITransactionImportService
{
    public async Task<ApiResponse<IReadOnlyList<TransactionImportSummaryResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var imports = await transactionImportRepository.GetAllAsync(cancellationToken);
        var data = imports.Select(TransactionImportMapper.ToSummaryResponse).ToList();
        return ApiResponse<IReadOnlyList<TransactionImportSummaryResponse>>.Ok(data);
    }

    public async Task<ApiResponse<TransactionImportDetailResponse>> GetByIdAsync(
        int transactionImportId,
        CancellationToken cancellationToken = default)
    {
        var import = await transactionImportRepository.GetByIdAsync(transactionImportId, cancellationToken);
        if (import is null)
        {
            return ApiResponse<TransactionImportDetailResponse>.Fail(
                $"Transaction import with id {transactionImportId} was not found.");
        }

        return ApiResponse<TransactionImportDetailResponse>.Ok(
            TransactionImportMapper.ToDetailResponse(import));
    }

    public async Task<ApiResponse<TransactionImportDetailResponse>> UploadCsvAsync(
        Stream csvStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return ApiResponse<TransactionImportDetailResponse>.Fail("A file name is required.");
        }

        IReadOnlyList<ParsedTransactionCsvRow> parsedRows;
        try
        {
            parsedRows = TransactionCsvParser.Parse(csvStream);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<TransactionImportDetailResponse>.Fail(ex.Message);
        }

        var categories = await accountCategoryRepository.GetAllAsync(cancellationToken);
        var categoryLookup = categories.ToDictionary(
            c => c.Name,
            c => c.AccountCategoryId,
            StringComparer.OrdinalIgnoreCase);

        var import = new TransactionImport
        {
            FileName = Path.GetFileName(fileName.Trim()),
            UploadedAt = DateTime.UtcNow,
            RowCount = parsedRows.Count,
            Transactions = parsedRows.Select(row =>
            {
                int? categoryId = null;
                if (!string.IsNullOrWhiteSpace(row.Category) &&
                    categoryLookup.TryGetValue(row.Category.Trim(), out var matchedId))
                {
                    categoryId = matchedId;
                }

                return new ImportedTransaction
                {
                    LineNumber = row.LineNumber,
                    Description = row.Description,
                    CategoryRaw = row.Category?.Trim(),
                    AccountCategoryId = categoryId,
                    Amount = row.Amount,
                    TransactionDate = row.TransactionDate,
                    PostingStatusRaw = row.PostingStatus?.Trim(),
                    BudgetPaymentStatusId = PostingStatusMapper.MapStatusId(row.PostingStatus),
                    Notes = row.Notes?.Trim()
                };
            }).ToList()
        };

        var created = await transactionImportRepository.AddAsync(import, cancellationToken);
        return ApiResponse<TransactionImportDetailResponse>.Ok(
            TransactionImportMapper.ToDetailResponse(created));
    }

    public async Task<ApiResponse<ImportedTransactionResponse>> UpdateImportedTransactionAsync(
        int importedTransactionId,
        UpdateImportedTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var transaction = await transactionImportRepository.GetImportedTransactionByIdAsync(
            importedTransactionId,
            cancellationToken);
        if (transaction is null)
        {
            return ApiResponse<ImportedTransactionResponse>.Fail(
                $"Imported transaction with id {importedTransactionId} was not found.");
        }

        if (request.IncomeSourceId.HasValue)
        {
            var exists = await incomeSourceRepository.ExistsAsync(
                request.IncomeSourceId.Value,
                cancellationToken);
            if (!exists)
            {
                return ApiResponse<ImportedTransactionResponse>.Fail(
                    $"Income source with id {request.IncomeSourceId.Value} was not found.");
            }
        }

        transaction.IncomeSourceId = request.IncomeSourceId;
        await transactionImportRepository.UpdateImportedTransactionAsync(transaction, cancellationToken);

        var updated = await transactionImportRepository.GetImportedTransactionByIdAsync(
            importedTransactionId,
            cancellationToken);

        return ApiResponse<ImportedTransactionResponse>.Ok(
            TransactionImportMapper.ToTransactionResponse(updated!));
    }

    public async Task<ApiResponse<TransactionImportDetailResponse>> UpdateImportAsync(
        int transactionImportId,
        UpdateTransactionImportRequest request,
        CancellationToken cancellationToken = default)
    {
        var import = await transactionImportRepository.GetByIdForUpdateAsync(
            transactionImportId,
            cancellationToken);
        if (import is null)
        {
            return ApiResponse<TransactionImportDetailResponse>.Fail(
                $"Transaction import with id {transactionImportId} was not found.");
        }

        if (request.IncomeSourceId.HasValue)
        {
            var exists = await incomeSourceRepository.ExistsAsync(
                request.IncomeSourceId.Value,
                cancellationToken);
            if (!exists)
            {
                return ApiResponse<TransactionImportDetailResponse>.Fail(
                    $"Income source with id {request.IncomeSourceId.Value} was not found.");
            }
        }

        import.IncomeSourceId = request.IncomeSourceId;
        await transactionImportRepository.UpdateImportAsync(import, cancellationToken);

        var updated = await transactionImportRepository.GetByIdAsync(
            transactionImportId,
            cancellationToken);

        return ApiResponse<TransactionImportDetailResponse>.Ok(
            TransactionImportMapper.ToDetailResponse(updated!));
    }
}
