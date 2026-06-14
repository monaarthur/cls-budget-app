# CLS Budget App — Database

PostgreSQL database for the budget API. This document is the **schema map and change workflow**. Business meaning of fields and rules live in [domain-rules.md](./domain-rules.md). The **source of truth** for what is deployed is EF Core code + migrations.

---

## Documentation split

| Topic | Document |
|-------|----------|
| What tables mean, validation, overdue logic, budget rules | [domain-rules.md](./domain-rules.md) |
| Stack, layers, API patterns | [architecture.md](./architecture.md) |
| Tables, tenancy, migrations, connection strings | **This file** (`database.md`) |
| `dotnet ef` commands, prerequisites | [../backend/src/CLS.Budget.Migration/README.md](../backend/src/CLS.Budget.Migration/README.md) |

When you add or change a table:

1. Describe **why** and **rules** in `domain-rules.md` (if user-facing or non-obvious).
2. Implement the entity and `BudgetDbContext` mapping in code.
3. Add an EF migration under `CLS.Budget.Migration`.
4. Update the **Entity inventory** section below if you add/remove a table.

---

## Technology

- **Engine:** PostgreSQL
- **ORM:** EF Core 9 (`BudgetDbContext`)
- **Migrations project:** `backend/src/CLS.Budget.Migration`
- **Connection string key:** `ConnectionStrings:BudgetDatabase` in `CLS.Budget.Api/appsettings*.json`

---

## Code locations

| Purpose | Path |
|---------|------|
| Domain entities | `backend/src/CLS.Budget.Domain/Entities/` |
| Fluent API / query filters / seed | `backend/src/CLS.Budget.Infrastructure/Persistance/BudgetDbContext.cs` |
| Lookup seed data | `backend/src/CLS.Budget.Infrastructure/Persistance/Seeding/LookupDataSeed.cs` |
| Default tenant constants | `backend/src/CLS.Budget.Domain/SeedTenant.cs` |
| Migration classes | `backend/src/CLS.Budget.Migration/Migrations/` |
| Ad-hoc SQL scripts | `backend/src/CLS.Budget.Migration/Scripts/` |

---

## Multi-tenancy

Households are modeled as **tenants**. Most budget data is scoped by `TenantId`.

### Tenant-owned tables (`ITenantOwned`)

Global query filters on `BudgetDbContext` restrict reads/writes to the current tenant (from JWT `tenant_id` or dev fallback). New rows get `TenantId` stamped on `SaveChanges`.

| Table (EF entity) | Notes |
|-------------------|--------|
| `Accounts` | Bills, cards, loans, utilities |
| `CreditCardDetail` | 1:1 with credit card accounts |
| `Budget` | Monthly budget (`BudgetModel`) |
| `BudgetPayments` | Line items per budget/account |
| `BudgetIncome` | Monthly income lines |
| `PaySchedule` | Pay frequency / dates per tenant |

### Identity (not tenant-filtered)

| Table | Notes |
|-------|--------|
| `Tenant` | Household; seeded default **MonaArthur** (`SeedTenant.DefaultTenantId`) |
| `AppUser` | Login user; unique `(TenantId, Email)` |
| `RefreshToken` | Hashed refresh tokens per user |

### Shared lookups (global, not `ITenantOwned`)

Reference data shared across tenants; seeded in migrations / `LookupDataSeed`.

| Table | Notes |
|-------|--------|
| `AccountCategory` | Account classification |
| `BudgetTemplate` | Template for new budgets |
| `BudgetPaymentStatus` | Pending, Scheduled, Paid, … |
| `PaymentSource` | How a payment was funded |
| `IncomeSource` | Job Income, Credit Cards, Business Income |
| `PayFrequencyType` | Weekly, biweekly, etc. |

---

## Entity inventory (core relationships)

```text
Tenant ──< AppUser ──< RefreshToken

Tenant ──< Account ──o CreditCardDetail (optional 1:1)
Tenant ──< Budget (BudgetModel) ──< BudgetPayment ──> Account
                              └──< BudgetIncome ──> IncomeSource
Tenant ──< PaySchedule ──> PayFrequencyType, IncomeSource

Budget ──> BudgetTemplate
BudgetPayment ──> BudgetPaymentStatus, PaymentSource, IncomeSource (optional)
```

**Budget membership:** `Budget.AccountIds` stores a comma-separated list of account IDs (see [domain-rules.md](./domain-rules.md) — junction table noted as future improvement).

---

## Notable columns (quick reference)

| Entity | Column | Purpose |
|--------|--------|---------|
| `Account` | `PaymentDay` | Due day of month (1–31) |
| `Account` | `IsCreditCard` | Drives credit-card UI/API |
| `Budget` | `Notes` | Free-text month notes |
| `Budget` | `AccountIds` | Accounts included in the month |
| `BudgetPayment` | `Amount`, `PaymentMade` | Budgeted vs paid amounts |
| `AppUser` | `Role` | `Owner` or `Member` |

---

## Migrations

All schema changes go through **`CLS.Budget.Migration`** only (no migrations in Api, Application, or Domain).

From `backend/`:

```bash
dotnet ef migrations add <Name> --project src/CLS.Budget.Migration --startup-project src/CLS.Budget.Api --output-dir Migrations
dotnet ef database update --project src/CLS.Budget.Migration --startup-project src/CLS.Budget.Api
```

Recent migrations (newest first):

| Migration | Summary |
|-----------|---------|
| `AddBudgetPaymentIncomeSource` | Optional income source on budget payments |
| `AddTenantIsolation` | `TenantId` on tenant-owned tables; backfill MonaArthur |
| `AddIdentityAndTenants` | Tenant, AppUser, RefreshToken; budget `Notes` |
| `AddBudgetIncome` | Monthly income tracking |
| `AddPaySchedule` | Pay schedules per tenant |
| `AddAccountPaymentDay` | Account payment due day |
| `InitialCreate` | Core budget/account/payment schema |

Full history: `backend/src/CLS.Budget.Migration/Migrations/`.

---

## Local development

1. PostgreSQL running locally.
2. Set `BudgetDatabase` in `CLS.Budget.Api/appsettings.Development.json`.
3. Apply migrations (command above).
4. Default tenant **MonaArthur** is seeded via `HasData` on `Tenant` in `BudgetDbContext`.

Import CLI can target a tenant: `--tenant <guid>` (defaults to `SeedTenant.DefaultTenantId`). See `CLS.Budget.Import` help.

Auth in Development often runs with `Auth:Enabled: false`; data still scopes to `Auth:DevTenantId` (MonaArthur) via the dev authentication handler.

---

## Related reading

- [domain-rules.md](./domain-rules.md) — accounts, payments, budgets, income, validation
- [architecture.md](./architecture.md) — Clean Architecture and persistence layer
- [CLS.Budget.Migration/README.md](../backend/src/CLS.Budget.Migration/README.md) — EF CLI setup
