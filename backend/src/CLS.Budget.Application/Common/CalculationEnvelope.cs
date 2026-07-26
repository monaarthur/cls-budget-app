using CLS.Budget.Domain.CreditCardEngine;

namespace CLS.Budget.Application.Common;

public sealed class CalculationEnvelope<T>
{
    public DateTime CalculatedOnUtc { get; init; }
    public string FormulaVersion { get; init; } = CreditCardMath.FormulaVersion;
    public IReadOnlyList<string> Assumptions { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public T Result { get; init; } = default!;

    public static CalculationEnvelope<T> Create(
        T result,
        IEnumerable<string>? assumptions = null,
        IEnumerable<string>? warnings = null) =>
        new()
        {
            CalculatedOnUtc = DateTime.UtcNow,
            FormulaVersion = CreditCardMath.FormulaVersion,
            Assumptions = assumptions?.ToList() ?? [],
            Warnings = warnings?.ToList() ?? [],
            Result = result
        };
}
