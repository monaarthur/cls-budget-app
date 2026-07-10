using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Application.Abstractions.Repositories;

public interface ITransactionImportRepository
{
    Task<IReadOnlyList<TransactionImport>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TransactionImport?> GetByIdAsync(int transactionImportId, CancellationToken cancellationToken = default);
    Task<TransactionImport> AddAsync(TransactionImport import, CancellationToken cancellationToken = default);
    Task<TransactionImport?> GetByIdForUpdateAsync(
        int transactionImportId,
        CancellationToken cancellationToken = default);
    Task UpdateImportAsync(TransactionImport import, CancellationToken cancellationToken = default);
    Task<ImportedTransaction?> GetImportedTransactionByIdAsync(
        int importedTransactionId,
        CancellationToken cancellationToken = default);
    Task UpdateImportedTransactionAsync(
        ImportedTransaction transaction,
        CancellationToken cancellationToken = default);
}
