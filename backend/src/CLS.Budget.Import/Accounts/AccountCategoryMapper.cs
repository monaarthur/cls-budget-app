namespace CLS.Budget.Import.Accounts;

internal static class AccountCategoryMapper
{
    /// <summary>
    /// Maps MonthlyPayments Excel category labels to seeded <see cref="CLS.Budget.Infrastructure.Persistance.Seeding.LookupDataSeed"/> ids.
    /// </summary>
    public static int Map(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return 5; // Subscription — Netflix, Gas, blank misc
        }

        return category.Trim().ToLowerInvariant() switch
        {
            "credit card" => 1,
            "mortgage" => 3,
            "utilities" or "utility" => 4,
            "investment and savings" or "investment & savings" => 6,
            "auto" => 2, // Loan
            "home" => 5, // Subscription until a Home category exists
            _ => 5
        };
    }
}
