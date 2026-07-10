using CLS.Budget.Application.Common;
using CLS.Budget.Application.TransactionImports.Dtos;

namespace CLS.Budget.Application.Abstractions.Services;

public interface ITransactionImportService
{
    Task<ApiResponse<IReadOnlyList<TransactionImportSummaryResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<TransactionImportDetailResponse>> GetByIdAsync(
        int transactionImportId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<TransactionImportDetailResponse>> UploadCsvAsync(
        Stream csvStream,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ImportedTransactionResponse>> UpdateImportedTransactionAsync(
        int importedTransactionId,
        UpdateImportedTransactionRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<TransactionImportDetailResponse>> UpdateImportAsync(
        int transactionImportId,
        UpdateTransactionImportRequest request,
        CancellationToken cancellationToken = default);
}
