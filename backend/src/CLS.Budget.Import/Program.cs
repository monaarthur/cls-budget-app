using CLS.Budget.Application;
using CLS.Budget.Application.Abstractions;
using CLS.Budget.Application.Accounts.Validators;
using CLS.Budget.Domain;
using CLS.Budget.Import;
using CLS.Budget.Import.Accounts;
using CLS.Budget.Import.MayBudgets;
using CLS.Budget.Import.PaymentSources;
using CLS.Budget.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var argsList = args.ToList();
if (argsList.Count == 0 || argsList.Contains("-h") || argsList.Contains("--help"))
{
    PrintHelp();
    return argsList.Count == 0 ? 1 : 0;
}

var command = argsList[0].ToLowerInvariant();

if (command == "validate")
{
    if (argsList.Count < 2)
    {
        Console.Error.WriteLine("Missing CSV file path.");
        PrintHelp();
        return 1;
    }

    var validatePath = Path.GetFullPath(argsList[1]);
    var report = CsvHeaderValidator.Validate(validatePath);
    PrintValidationReport(report);
    return report.IsValid ? 0 : 1;
}

if (command is not ("accounts" or "payment-sources" or "may-budget" or "payment-days"))
{
    Console.Error.WriteLine($"Unknown command: {argsList[0]}");
    PrintHelp();
    return 1;
}

if (argsList.Count < 2)
{
    Console.Error.WriteLine("Missing CSV file path.");
    PrintHelp();
    return 1;
}

var csvPath = Path.GetFullPath(argsList[1]);
if (!File.Exists(csvPath))
{
    Console.Error.WriteLine($"File not found: {csvPath}");
    return 1;
}

var activeOnly = argsList.Contains("--active-only");
var dryRun = argsList.Contains("--dry-run");

var tenantId = SeedTenant.DefaultTenantId;
var tenantArgIndex = argsList.FindIndex(a => string.Equals(a, "--tenant", StringComparison.OrdinalIgnoreCase));
if (tenantArgIndex >= 0
    && (tenantArgIndex + 1 >= argsList.Count || !Guid.TryParse(argsList[tenantArgIndex + 1], out tenantId)))
{
    Console.Error.WriteLine("Invalid or missing value for --tenant (expected a GUID).");
    return 1;
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.SetBasePath(AppContext.BaseDirectory);
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        config.AddJsonFile(
            $"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
            optional: true,
            reloadOnChange: false);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplication();
        services.AddInfrastructure(context.Configuration);
        services.AddSingleton<ITenantContext>(new CliTenantContext(tenantId));
        services.AddSingleton<CreateAccountRequestValidator>();
        services.AddScoped<AccountCsvImporter>();
        services.AddScoped<PaymentDayUpdateImporter>();
        services.AddScoped<PaymentSourceCsvImporter>();
        services.AddScoped<MayBudgetCsvImporter>();
    })
    .Build();

using var scope = host.Services.CreateScope();

Console.WriteLine($"Target tenant: {tenantId}");
Console.WriteLine();

if (command == "accounts")
{
    var importer = scope.ServiceProvider.GetRequiredService<AccountCsvImporter>();
    Console.WriteLine($"Importing accounts from: {csvPath}");
    Console.WriteLine($"  Active only: {activeOnly}");
    Console.WriteLine($"  Dry run:     {dryRun}");
    Console.WriteLine();

    var summary = await importer.ImportAsync(csvPath, activeOnly, dryRun);
    PrintSummary(summary.Imported, summary.Skipped, summary.Failed, summary.Messages);
    return summary.Failed > 0 ? 1 : 0;
}

if (command == "payment-days")
{
    var importer = scope.ServiceProvider.GetRequiredService<PaymentDayUpdateImporter>();
    Console.WriteLine($"Updating account payment days from: {csvPath}");
    Console.WriteLine($"  Dry run: {dryRun}");
    Console.WriteLine();

    var summary = await importer.ImportAsync(csvPath, dryRun);
    PrintPaymentDaySummary(summary);
    return summary.Failed > 0 ? 1 : 0;
}

if (command == "payment-sources")
{
    var importer = scope.ServiceProvider.GetRequiredService<PaymentSourceCsvImporter>();
    Console.WriteLine($"Importing payment sources from: {csvPath}");
    Console.WriteLine($"  Dry run: {dryRun}");
    Console.WriteLine();

    var summary = await importer.ImportAsync(csvPath, dryRun);
    PrintSummary(summary.Imported, summary.Skipped, summary.Failed, summary.Messages);
    return summary.Failed > 0 ? 1 : 0;
}

{
    var importer = scope.ServiceProvider.GetRequiredService<MayBudgetCsvImporter>();
    var options = new MayBudgetImportOptions
    {
        Month = ParseIntOption(argsList, "--month", 5),
        Year = ParseIntOption(argsList, "--year", 2026),
        BudgetTemplateId = ParseIntOption(argsList, "--template-id", 1),
        DryRun = dryRun,
        ActiveOnly = activeOnly
    };

    Console.WriteLine($"Importing May budget from: {csvPath}");
    Console.WriteLine($"  Month:        {options.Month}");
    Console.WriteLine($"  Year:         {options.Year}");
    Console.WriteLine($"  Template id:  {options.BudgetTemplateId}");
    Console.WriteLine($"  Active only:  {options.ActiveOnly}");
    Console.WriteLine($"  Dry run:      {options.DryRun}");
    Console.WriteLine();

    var summary = await importer.ImportAsync(csvPath, options);
    PrintMayBudgetSummary(summary);
    return summary.Failed > 0 ? 1 : 0;
}

static int ParseIntOption(List<string> argsList, string name, int defaultValue)
{
    for (var i = 0; i < argsList.Count; i++)
    {
        if (!string.Equals(argsList[i], name, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (i + 1 >= argsList.Count)
        {
            return defaultValue;
        }

        return int.TryParse(argsList[i + 1], out var value) ? value : defaultValue;
    }

    return defaultValue;
}

static void PrintSummary(int imported, int skipped, int failed, IReadOnlyList<string> messages)
{
    Console.WriteLine($"Imported: {imported}");
    Console.WriteLine($"Skipped:  {skipped}");
    Console.WriteLine($"Failed:   {failed}");

    foreach (var message in messages)
    {
        Console.WriteLine($"  - {message}");
    }
}

static void PrintPaymentDaySummary(PaymentDayUpdateSummary summary)
{
    Console.WriteLine($"Updated:   {summary.Updated}");
    Console.WriteLine($"Unchanged: {summary.Unchanged}");
    Console.WriteLine($"Skipped:   {summary.Skipped}");
    Console.WriteLine($"Not found: {summary.NotFound}");
    Console.WriteLine($"Failed:    {summary.Failed}");

    foreach (var message in summary.Messages)
    {
        Console.WriteLine($"  - {message}");
    }
}

static void PrintMayBudgetSummary(MayBudgetImportSummary summary)
{
    Console.WriteLine($"Imported: {summary.Imported}");
    Console.WriteLine($"Updated:  {summary.Updated}");
    Console.WriteLine($"Skipped:  {summary.Skipped}");
    Console.WriteLine($"Failed:   {summary.Failed}");

    foreach (var message in summary.Messages)
    {
        Console.WriteLine($"  - {message}");
    }
}

static void PrintValidationReport(CsvValidationReport report)
{
    Console.WriteLine($"File: {report.FilePath}");
    Console.WriteLine($"Data rows (after skipping metadata): {report.DataRowCount}");
    Console.WriteLine();
    Console.WriteLine("Headers in file:");
    for (var i = 0; i < report.FileHeaders.Count; i++)
    {
        var norm = i < report.NormalizedHeaders.Count ? report.NormalizedHeaders[i] : "";
        Console.WriteLine($"  {i + 1,2}. \"{report.FileHeaders[i]}\"  ->  {norm}");
    }

    Console.WriteLine();
    if (report.IsValid)
    {
        Console.WriteLine("Result: OK — headers match the import model.");
        return;
    }

    Console.WriteLine("Result: ISSUES FOUND");
    foreach (var issue in report.Issues)
    {
        Console.WriteLine($"  - {issue}");
    }
}

static void PrintHelp()
{
    Console.WriteLine("""
        CLS.Budget.Import — load reference data via application services.

        Usage:
          dotnet run --project src/CLS.Budget.Import -- validate <accounts.csv>
          dotnet run --project src/CLS.Budget.Import -- accounts <file.csv> [--active-only] [--dry-run] [--tenant <guid>]
          dotnet run --project src/CLS.Budget.Import -- payment-days <file.csv> [--dry-run] [--tenant <guid>]
          dotnet run --project src/CLS.Budget.Import -- payment-sources <file.csv> [--dry-run] [--tenant <guid>]
          dotnet run --project src/CLS.Budget.Import -- may-budget <file.csv> [--month 5] [--year 2026] [--template-id 1] [--active-only] [--dry-run] [--tenant <guid>]

        --tenant <guid>: target tenant for the import. Defaults to the seeded tenant when omitted.

        May budget CSV: spreadsheet export with a header row containing "Account / Bill" and "Amount To Pay".
        Creates the budget for the month/year if missing, then inserts or updates BudgetPayment rows.

        Payment days CSV: header row with account name, number, and payment date/day.
        Matches existing accounts and updates PaymentDay only (1–31).

        Example:
          dotnet run --project src/CLS.Budget.Import -- may-budget data\\MayBudget.csv --dry-run
          dotnet run --project src/CLS.Budget.Import -- may-budget data\\MayBudget.csv
        """);
}
