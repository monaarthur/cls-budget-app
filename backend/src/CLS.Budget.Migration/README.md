# CLS.Budget.Migration

Canonical EF Core migrations project for CLS Budget App.

## Commands

Run from the `backend` directory:

```bash
dotnet ef migrations add <MigrationName> --project src/CLS.Budget.Migration --startup-project src/CLS.Budget.Api --output-dir Migrations
dotnet ef database update --project src/CLS.Budget.Migration --startup-project src/CLS.Budget.Api
```

The Api project does **not** reference the Migration project (avoids build errors and keeps the API free of migration tooling). `Microsoft.EntityFrameworkCore.Design` on the Api is only needed when using `--startup-project src/CLS.Budget.Api`.

## Configuration

Uses `ConnectionStrings:BudgetDatabase` in `appsettings.json` (design-time) or the Api project's `appsettings` when using `--startup-project src/CLS.Budget.Api`.

The legacy `migrations/CLS.Budget.Migrations` folder is deprecated and removed from the solution.

## Prerequisites

1. Install the EF CLI tool (once per machine):

   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. PostgreSQL running locally (or update connection strings).

3. Match `BudgetDatabase` in `src/CLS.Budget.Api/appsettings.Development.json` and `src/CLS.Budget.Migration/appsettings.json` to your server.
