using System.Globalization;
using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;

namespace CLS.Budget.Import.Accounts;

internal sealed class PaymentDayUpdateImporter(IAccountRepository accountRepository)
{
    public async Task<PaymentDayUpdateSummary> ImportAsync(
        string csvPath,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var rows = PaymentDayCsvReader.Read(csvPath);
        var accounts = await accountRepository.GetAllAsync(cancellationToken);
        var summary = new PaymentDayUpdateSummary();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var line = i + 2;

            if (string.IsNullOrWhiteSpace(row.PaymentDate))
            {
                summary.Skipped++;
                continue;
            }

            if (!TryParsePaymentDay(row.PaymentDate, out var paymentDay))
            {
                summary.Failed++;
                summary.Messages.Add(
                    $"Line {line} ({FormatRowLabel(row)}): invalid payment date \"{row.PaymentDate}\" — use day 1–31 or a date.");
                continue;
            }

            var match = FindAccount(accounts, row);
            if (match is null)
            {
                summary.NotFound++;
                summary.Messages.Add($"Line {line} ({FormatRowLabel(row)}): no matching account found.");
                continue;
            }

            if (match.PaymentDay == paymentDay)
            {
                summary.Unchanged++;
                continue;
            }

            if (dryRun)
            {
                summary.Updated++;
                continue;
            }

            var tracked = await accountRepository.GetByIdAsync(match.AccountId, cancellationToken);
            if (tracked is null)
            {
                summary.Failed++;
                summary.Messages.Add($"Line {line} ({FormatRowLabel(row)}): account id {match.AccountId} disappeared.");
                continue;
            }

            tracked.PaymentDay = paymentDay;
            await accountRepository.UpdateAsync(tracked, cancellationToken);
            summary.Updated++;
        }

        return summary;
    }

    private static Account? FindAccount(IReadOnlyList<Account> accounts, PaymentDayCsvRow row)
    {
        var name = NormalizeName(row.Name);
        var number = NormalizeNumber(row.Number);

        if (!string.IsNullOrEmpty(number) && !string.IsNullOrEmpty(name))
        {
            return accounts.FirstOrDefault(a =>
                NormalizeNumber(a.Number) == number
                && NormalizeName(a.Name) == name);
        }

        if (!string.IsNullOrEmpty(number))
        {
            var byNumber = accounts
                .Where(a => NormalizeNumber(a.Number) == number)
                .ToList();

            return byNumber.Count == 1 ? byNumber[0] : null;
        }

        if (!string.IsNullOrEmpty(name))
        {
            var byName = accounts
                .Where(a => NormalizeName(a.Name) == name)
                .ToList();

            return byName.Count == 1 ? byName[0] : null;
        }

        return null;
    }

    internal static bool TryParsePaymentDay(string? value, out int paymentDay)
    {
        paymentDay = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out paymentDay)
            && paymentDay is >= 1 and <= 31)
        {
            return true;
        }

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            || DateTime.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
        {
            paymentDay = date.Day;
            return paymentDay is >= 1 and <= 31;
        }

        return false;
    }

    private static string NormalizeName(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();

    private static string NormalizeNumber(string? value) =>
        (value ?? string.Empty).Trim();

    private static string FormatRowLabel(PaymentDayCsvRow row)
    {
        var name = row.Name?.Trim();
        var number = row.Number?.Trim();

        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(number))
        {
            return $"{name} ({number})";
        }

        return name ?? number ?? "unknown";
    }
}

internal sealed class PaymentDayUpdateSummary
{
    public int Updated { get; set; }
    public int Unchanged { get; set; }
    public int Skipped { get; set; }
    public int NotFound { get; set; }
    public int Failed { get; set; }
    public List<string> Messages { get; } = [];
}
