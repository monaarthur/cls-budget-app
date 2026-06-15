# CLS.Budget.Migration

Canonical EF Core migrations project for CLS Budget App.

## Commands

Run from the `backend` directory:

```bash
dotnet ef migrations add <MigrationName> --project src/CLS.Budget.Migration --startup-project src/CLS.Budget.Migration --output-dir Migrations
dotnet ef database update --project src/CLS.Budget.Migration --startup-project src/CLS.Budget.Migration
```

The Api project does **not** reference the Migration project (avoids build errors and keeps the API free of migration tooling). Use `--startup-project src/CLS.Budget.Migration` so EF loads the migration assembly from the Migration project output.

## Configuration

Uses `ConnectionStrings:BudgetDatabase` in `appsettings.json` for local design-time runs. For Supabase (local or CI), pass the connection string with `--connection` and use `--startup-project src/CLS.Budget.Migration` so EF loads configuration from this project.

The legacy `migrations/CLS.Budget.Migrations` folder is deprecated and removed from the solution.

## Prerequisites

1. Install the EF CLI tool (once per machine):

   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. PostgreSQL running locally (or update connection strings).

3. Match `BudgetDatabase` in `src/CLS.Budget.Api/appsettings.Development.json` and `src/CLS.Budget.Migration/appsettings.json` to your server.
