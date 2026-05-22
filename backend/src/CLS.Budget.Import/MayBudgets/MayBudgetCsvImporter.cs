using System.Globalization;
using CLS.Budget.Application.Abstractions.Repositories;
using CLS.Budget.Domain.Entities;
using CLS.Budget.Import.Parsing;

namespace CLS.Budget.Import.MayBudgets;

internal sealed class MayBudgetCsvImporter(
    IBudgetRepository budgetRepository,
    IBudgetTemplateRepository budgetTemplateRepository,
    IAccountRepository accountRepository,
    IPaymentRepository paymentRepository,
    IPaymentSourceRepository paymentSourceRepository)
{
    public async Task<MayBudgetImportSummary> ImportAsync(
        string csvPath,
        MayBudgetImportOptions options,
        CancellationToken cancellationToken = default)
    {
        var rows = MayBudgetCsvReader.Read(csvPath);
        var summary = new MayBudgetImportSummary();

        var accounts = await accountRepository.GetAllAsync(cancellationToken);
        var accountLookup = AccountNameMatcher.BuildLookup(accounts);

        var budget = await budgetRepository.GetByMonthYearWithPaymentsAsync(
            options.Month,
            options.Year,
            cancellationToken);

        var createdBudget = false;
        if (budget is null)
        {
            var template = await budgetTemplateRepository.GetByIdAsync(
                options.BudgetTemplateId,
                cancellationToken);
            if (template is null)
            {
                summary.Failed++;
                summary.Messages.Add(
                    $"Budget template {options.BudgetTemplateId} was not found.");
                return summary;
            }

            var start = new DateTime(options.Year, options.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1).AddDays(-1);
            budget = new BudgetModel
            {
                Name = $"{CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(options.Month)} {options.Year} Budget",
                StartPeriod = start,
                EndPeriod = end,
                BudgetTemplateId = options.BudgetTemplateId
            };

            if (!options.DryRun)
            {
                budget = await budgetRepository.AddAsync(budget, cancellationToken);
            }

            createdBudget = true;
            summary.Messages.Add(
                options.DryRun
                    ? $"Would create budget for {options.Month}/{options.Year}."
                    : $"Created budget id {budget.BudgetId} for {options.Month}/{options.Year}.");
        }
        else
        {
            summary.Messages.Add($"Using existing budget id {budget.BudgetId}.");
        }

        var budgetId = budget.BudgetId;
        var defaultPaymentDate = new DateTime(options.Year, options.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var headerLineOffset = MayBudgetCsvReader.FindHeaderLineNumber(csvPath) + 2;

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var line = headerLineOffset + i;
            var accountName = row.AccountBill!.Trim();

            if (options.ActiveOnly &&
                !string.Equals(row.Active?.Trim(), "Yes", StringComparison.OrdinalIgnoreCase))
            {
                summary.Skipped++;
                continue;
            }

            if (!AccountNameMatcher.TryResolve(accountLookup, accounts, accountName, out var account))
            {
                summary.Skipped++;
                summary.Messages.Add($"Line {line} ({accountName}): no matching account, skipped.");
                continue;
            }

            var amount = ResolveAmount(row);
            var datePaid = ParseDatePaid(row.DatePaid);
            var paymentDate = datePaid ?? defaultPaymentDate;
            var statusId = BudgetPaymentStatusMapper.MapStatusId(row.Status);
            var isCleared = BudgetPaymentStatusMapper.IsCleared(row.Status, datePaid);

            int? paymentSourceId = null;
            var paymentAccount = row.PaymentAccount?.Trim();
            if (!string.IsNullOrWhiteSpace(paymentAccount))
            {
                var source = await paymentSourceRepository.GetByNameAsync(paymentAccount, cancellationToken);
                if (source is null)
                {
                    summary.Failed++;
                    summary.Messages.Add(
                        $"Line {line} ({accountName}): payment source \"{paymentAccount}\" not found.");
                    continue;
                }

                paymentSourceId = source.PaymentSourceId;
            }

            if (options.DryRun)
            {
                summary.Imported++;
                continue;
            }

            var existing = await paymentRepository.GetByBudgetAndAccountAsync(
                budgetId,
                account.AccountId,
                cancellationToken);

            if (existing is null)
            {
                await paymentRepository.AddAsync(new BudgetPayment
                {
                    BudgetId = budgetId,
                    AccountId = account.AccountId,
                    PaymentMade = amount,
                    Amount = amount,
                    BudgetPaymentStatusId = statusId,
                    IsCleared = isCleared,
                    PaymentDate = paymentDate,
                    ClearedDate = isCleared ? datePaid : null,
                    PaymentSourceId = paymentSourceId
                }, cancellationToken);
                summary.Imported++;
            }
            else
            {
                existing.PaymentMade = amount;
                existing.Amount = amount;
                existing.BudgetPaymentStatusId = statusId;
                existing.IsCleared = isCleared;
                existing.PaymentDate = paymentDate;
                existing.ClearedDate = isCleared ? datePaid : null;
                existing.PaymentSourceId = paymentSourceId;
                await paymentRepository.UpdateAsync(existing, cancellationToken);
                summary.Updated++;
            }
        }

        if (createdBudget && options.DryRun)
        {
            summary.Messages.Add("Dry run: budget row was not persisted.");
        }

        return summary;
    }

    private static decimal ResolveAmount(MayBudgetCsvRow row)
    {
        var amountToPay = MoneyParser.ParseNullable(row.AmountToPay);
        if (amountToPay.HasValue)
        {
            return amountToPay.Value;
        }

        var monthlyOverride = MoneyParser.ParseNullable(row.MonthlyDueOverride);
        if (monthlyOverride.HasValue)
        {
            return monthlyOverride.Value;
        }

        return MoneyParser.ParseOrDefault(row.ApproxAmount);
    }

    private static DateTime? ParseDatePaid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
        }

        // Fix common typo e.g. 5/12026 -> 5/1/2026
        var digitsOnly = trimmed.Replace("/", "", StringComparison.Ordinal);
        if (digitsOnly.Length >= 7 &&
            int.TryParse(digitsOnly[..^4], out var month) &&
            int.TryParse(digitsOnly[^4..], out var year) &&
            month is >= 1 and <= 12)
        {
            var dayDigits = digitsOnly[..^4];
            var day = dayDigits.Length > 2
                ? int.Parse(dayDigits[^2..], CultureInfo.InvariantCulture)
                : int.Parse(dayDigits, CultureInfo.InvariantCulture);
            if (day is >= 1 and <= 31)
            {
                return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
            }
        }

        return null;
    }
}

internal sealed class MayBudgetImportSummary
{
    public int Imported { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<string> Messages { get; } = [];
}
