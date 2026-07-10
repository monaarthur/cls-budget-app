using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace CLS.Budget.Infrastructure.Repositories;

public sealed class TransactionImportRepository(BudgetDbContext dbContext) : ITransactionImportRepository
{
    private IQueryable<TransactionImport> ImportsWithDetails =>
        dbContext.TransactionImports
            .Include(i => i.IncomeSource)
            .Include(i => i.Transactions)
                .ThenInclude(t => t.AccountCategory)
            .Include(i => i.Transactions)
                .ThenInclude(t => t.BudgetPaymentStatus)
            .Include(i => i.Transactions)
                .ThenInclude(t => t.IncomeSource);

    public async Task<IReadOnlyList<TransactionImport>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.TransactionImports
            .AsNoTracking()
            .OrderByDescending(i => i.UploadedAt)
            .ToListAsync(cancellationToken);

    public async Task<TransactionImport?> GetByIdAsync(
        int transactionImportId,
        CancellationToken cancellationToken = default) =>
        await ImportsWithDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.TransactionImportId == transactionImportId, cancellationToken);

    public async Task<TransactionImport> AddAsync(
        TransactionImport import,
        CancellationToken cancellationToken = default)
    {
        dbContext.TransactionImports.Add(import);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(import.TransactionImportId, cancellationToken))!;
    }

    public async Task<TransactionImport?> GetByIdForUpdateAsync(
        int transactionImportId,
        CancellationToken cancellationToken = default) =>
        await dbContext.TransactionImports
            .FirstOrDefaultAsync(i => i.TransactionImportId == transactionImportId, cancellationToken);

    public async Task UpdateImportAsync(
        TransactionImport import,
        CancellationToken cancellationToken = default)
    {
        dbContext.TransactionImports.Update(import);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ImportedTransaction?> GetImportedTransactionByIdAsync(
        int importedTransactionId,
        CancellationToken cancellationToken = default) =>
        await dbContext.ImportedTransactions
            .Include(t => t.IncomeSource)
            .Include(t => t.AccountCategory)
            .Include(t => t.BudgetPaymentStatus)
            .FirstOrDefaultAsync(t => t.ImportedTransactionId == importedTransactionId, cancellationToken);

    public async Task UpdateImportedTransactionAsync(
        ImportedTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        dbContext.ImportedTransactions.Update(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
