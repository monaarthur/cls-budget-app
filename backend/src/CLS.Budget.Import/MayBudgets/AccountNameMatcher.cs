using System.Text.RegularExpressions;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Import.MayBudgets;

internal static class AccountNameMatcher
{
    private static readonly Regex AutoPaySuffix = new(
        @"\s*\(Auto\s*Pay\)\s*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ParentheticalSuffix = new(
        @"\s*\([^)]*\)",
        RegexOptions.Compiled);

    public static IReadOnlyDictionary<string, Account> BuildLookup(IReadOnlyList<Account> accounts)
    {
        var lookup = new Dictionary<string, Account>(StringComparer.OrdinalIgnoreCase);

        foreach (var account in accounts)
        {
            AddKey(lookup, account.Name, account);
            var normalized = NormalizeName(account.Name);
            if (!string.IsNullOrEmpty(normalized))
            {
                AddKey(lookup, normalized, account);
            }
        }

        return lookup;
    }

    public static bool TryResolve(
        IReadOnlyDictionary<string, Account> lookup,
        IReadOnlyList<Account> accounts,
        string accountBill,
        out Account account)
    {
        var trimmed = accountBill.Trim();
        if (lookup.TryGetValue(trimmed, out account!))
        {
            return true;
        }

        var normalized = NormalizeName(trimmed);
        if (!string.IsNullOrEmpty(normalized) && lookup.TryGetValue(normalized, out account!))
        {
            return true;
        }

        if (TryLongestPrefixMatch(accounts, normalized, out account))
        {
            return true;
        }

        account = null!;
        return false;
    }

    private static bool TryLongestPrefixMatch(
        IReadOnlyList<Account> accounts,
        string normalizedBill,
        out Account account)
    {
        account = null!;
        var bestLength = 0;

        foreach (var candidate in accounts)
        {
            var normalizedAccount = NormalizeName(candidate.Name);
            if (normalizedAccount.Length == 0 ||
                normalizedBill.Length < normalizedAccount.Length ||
                !normalizedBill.StartsWith(normalizedAccount, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (normalizedBill.Length > normalizedAccount.Length &&
                normalizedBill[normalizedAccount.Length] != ' ')
            {
                continue;
            }

            if (normalizedAccount.Length > bestLength)
            {
                bestLength = normalizedAccount.Length;
                account = candidate;
            }
        }

        return account is not null;
    }

    private static void AddKey(Dictionary<string, Account> lookup, string key, Account account)
    {
        if (!lookup.ContainsKey(key))
        {
            lookup[key] = account;
        }
    }

    private static string NormalizeName(string name)
    {
        var result = AutoPaySuffix.Replace(name.Trim(), " ");
        result = ParentheticalSuffix.Replace(result, "");
        return result.Trim();
    }
}
