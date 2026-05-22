using System.Globalization;
using System.Text.Json;

namespace CLS.Budget.Application.Budgets;

public static class BudgetTemplateAccountIdsParser
{
    public static IReadOnlyList<int> Parse(string? accountIds)
    {
        if (string.IsNullOrWhiteSpace(accountIds))
        {
            return [];
        }

        var trimmed = accountIds.Trim();
        if (trimmed.StartsWith('['))
        {
            var ids = JsonSerializer.Deserialize<List<int>>(trimmed);
            return ids ?? [];
        }

        return trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => int.Parse(part, CultureInfo.InvariantCulture))
            .ToList();
    }

    public static string Serialize(IReadOnlyList<int> accountIds) =>
        JsonSerializer.Serialize(accountIds);
}
