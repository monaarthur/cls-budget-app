# SQL seed scripts

## SeedAccountCategoryAndBudgetTemplate.sql

Seeds lookup rows required by the API:

| Id | AccountCategory |
|----|-----------------|
| 1 | Credit Card (used by `/api/v1/creditcards`) |
| 2–7 | Loan, Mortgage, Utility, Subscription, Savings, Checking |

| Id | BudgetTemplate |
|----|----------------|
| 1 | Monthly Household Budget (default for new budgets) |

### Option A — EF migration (recommended)

Seed data is defined in `LookupDataSeed` on `BudgetDbContext`. After tables exist, run:

```bash
cd backend
dotnet ef migrations add SeedAccountCategoryAndBudgetTemplate --project src/CLS.Budget.Migration --startup-project src/CLS.Budget.Migration --output-dir Migrations
dotnet ef database update --project src/CLS.Budget.Migration --startup-project src/CLS.Budget.Migration
```

### Option B — Run SQL manually

```bash
psql -h localhost -U postgres -d cls_budget -f src/CLS.Budget.Migration/Scripts/SeedAccountCategoryAndBudgetTemplate.sql
```

Adjust connection details for your environment.
