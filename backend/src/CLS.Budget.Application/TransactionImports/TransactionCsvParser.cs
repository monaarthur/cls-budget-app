using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace CLS.Budget.Application.TransactionImports;

internal static class TransactionCsvParser
{
    private static readonly string[] DescriptionHeaders =
        ["account bill", "account / bill", "description", "memo", "payee", "merchant"];

    private static readonly string[] CategoryHeaders = ["category"];

    private static readonly string[] AmountHeaders =
        ["amount to pay", "amount", "debit", "credit", "transaction amount"];

    private static readonly string[] DateHeaders =
        ["date paid", "date", "transaction date", "posted date"];

    private static readonly string[] PostingStatusHeaders =
        ["posting status", "postingstatus", "post status"];

    private static readonly string[] NotesHeaders = ["notes", "note"];

    public static IReadOnlyList<ParsedTransactionCsvRow> Parse(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        if (lines.Count == 0)
        {
            throw new InvalidOperationException("The CSV file is empty.");
        }

        var headerLineIndex = FindHeaderLineIndex(lines);
        if (headerLineIndex < 0)
        {
            throw new InvalidOperationException(
                "Could not find a header row. Expected columns such as Description (or Account / Bill) and Amount.");
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => NormalizeHeader(args.Header)
        };

        using var csvReader = new StringReader(string.Join(Environment.NewLine, lines.Skip(headerLineIndex)));
        using var csv = new CsvReader(csvReader, config);
        csv.Read();
        csv.ReadHeader();

        var headers = csv.HeaderRecord ?? [];
        var descriptionIndex = FindColumnIndex(headers, DescriptionHeaders);
        var categoryIndex = FindColumnIndex(headers, CategoryHeaders);
        var amountIndex = FindColumnIndex(headers, AmountHeaders);
        var dateIndex = FindColumnIndex(headers, DateHeaders);
        var postingStatusIndex = FindColumnIndex(headers, PostingStatusHeaders);
        var notesIndex = FindColumnIndex(headers, NotesHeaders);

        if (descriptionIndex < 0)
        {
            throw new InvalidOperationException(
                "Missing description column. Use Account / Bill, Description, Memo, or Payee.");
        }

        if (amountIndex < 0)
        {
            throw new InvalidOperationException(
                "Missing amount column. Use Amount To Pay or Amount.");
        }

        var rows = new List<ParsedTransactionCsvRow>();
        var lineNumber = headerLineIndex + 2;

        while (csv.Read())
        {
            var description = GetField(csv, descriptionIndex)?.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                lineNumber++;
                continue;
            }

            rows.Add(new ParsedTransactionCsvRow
            {
                LineNumber = lineNumber,
                Description = description,
                Category = GetField(csv, categoryIndex),
                Amount = ParseAmount(GetField(csv, amountIndex)),
                TransactionDate = ParseDate(GetField(csv, dateIndex)),
                PostingStatus = GetField(csv, postingStatusIndex),
                Notes = GetField(csv, notesIndex)
            });

            lineNumber++;
        }

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("No transaction rows were found in the CSV.");
        }

        return rows;
    }

    private static int FindHeaderLineIndex(IReadOnlyList<string> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var lower = lines[i].ToLowerInvariant();
            var hasDescription = DescriptionHeaders.Any(h => lower.Contains(h, StringComparison.Ordinal));
            var hasAmount = AmountHeaders.Any(h => lower.Contains(h, StringComparison.Ordinal));
            if (hasDescription && hasAmount)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindColumnIndex(IReadOnlyList<string> headers, IEnumerable<string> accepted)
    {
        var acceptedSet = accepted.Select(NormalizeHeader).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            if (acceptedSet.Contains(NormalizeHeader(headers[i])))
            {
                return i;
            }
        }

        return -1;
    }

    private static string? GetField(CsvReader csv, int index)
    {
        if (index < 0)
        {
            return null;
        }

        return csv.TryGetField(index, out string? value) ? value : null;
    }

    private static string NormalizeHeader(string header)
    {
        var normalized = header.Trim()
            .Replace("/", " ", StringComparison.Ordinal)
            .Replace("#", "number", StringComparison.Ordinal);

        while (normalized.Contains("  ", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
        }

        return normalized;
    }

    private static decimal ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        var cleaned = value.Trim()
            .Replace("$", "", StringComparison.Ordinal)
            .Replace(",", "", StringComparison.Ordinal)
            .Replace("(", "-", StringComparison.Ordinal)
            .Replace(")", "", StringComparison.Ordinal);

        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? Math.Abs(amount)
            : 0m;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
        }

        return null;
    }
}
