using System.Text.Json.Serialization;
using CLS.Budget.Application.Common.Serialization;

namespace CLS.Budget.Application.TransactionImports.Dtos;

public sealed class TransactionImportSummaryResponse
{
    public int TransactionImportId { get; init; }
    public string FileName { get; init; } = null!;

    [JsonConverter(typeof(DateOnlyUtcJsonConverter))]
    public DateTime UploadedAt { get; init; }

    public int RowCount { get; init; }
    public int? IncomeSourceId { get; init; }
    public string? IncomeSourceName { get; init; }
}

public sealed class ImportedTransactionResponse
{
    public int ImportedTransactionId { get; init; }
    public int LineNumber { get; init; }
    public string Description { get; init; } = null!;
    public string? CategoryRaw { get; init; }
    public int? AccountCategoryId { get; init; }
    public string? AccountCategoryName { get; init; }
    public decimal Amount { get; init; }

    [JsonConverter(typeof(NullableDateOnlyUtcJsonConverter))]
    public DateTime? TransactionDate { get; init; }

    public string? PostingStatusRaw { get; init; }
    public int BudgetPaymentStatusId { get; init; }
    public string BudgetPaymentStatusName { get; init; } = null!;
    public int? IncomeSourceId { get; init; }
    public string? IncomeSourceName { get; init; }
    public string? Notes { get; init; }
}

public sealed class UpdateImportedTransactionRequest
{
    public int? IncomeSourceId { get; init; }
}

public sealed class UpdateTransactionImportRequest
{
    public int? IncomeSourceId { get; init; }
}

public sealed class TransactionImportDetailResponse
{
    public int TransactionImportId { get; init; }
    public string FileName { get; init; } = null!;

    [JsonConverter(typeof(DateOnlyUtcJsonConverter))]
    public DateTime UploadedAt { get; init; }

    public int RowCount { get; init; }
    public int? IncomeSourceId { get; init; }
    public string? IncomeSourceName { get; init; }
    public IReadOnlyList<ImportedTransactionResponse> Transactions { get; init; } = [];
}
