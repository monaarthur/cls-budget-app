\# CLS Budget App — Domain Rules



\# Accounts



An Account represents:



\* Credit card

\* Loan

\* Mortgage

\* Utility

\* Subscription

\* Savings account

\* Checking account



Each Account must contain:



\* Name

\* Payment amount

\* Due date

\* Current balance or amount owed



\---



\# Payment Rules



\## Payment Statuses



Valid statuses (lookup table `BudgetPaymentStatus`, FK on `BudgetPayment.BudgetPaymentStatusId`):



\* Pending (1)

\* Scheduled (2)

\* Paid (3)

\* Failed (4)

\* Overdue (5)



New budget payments created from a template default to **Pending**.



\---



\## Overdue Logic



A payment becomes overdue when:



\* Current date > due date

\* Payment status != Paid



\---



\## Due Soon Logic



A payment is due soon when:



\* Due date is within 7 days

\* Status != Paid



\---



\## Interest Calculations



Interest calculations should:



\* Support APR percentages

\* Be configurable

\* Be calculated in backend services



\---



\# Budget Rules



Budgets are monthly.



\## Account membership (`AccountIds`)



Both **BudgetTemplate** and **Budget** store which accounts belong in scope using an `AccountIds` column:



\* **Storage:** nullable `text` on `BudgetTemplates` and `Budget` (table name `Budget`)

\* **Format:** JSON array of integers, e.g. `[1, 2, 5, 12]`, or comma-separated `1, 2, 5, 12` (same parser as templates)

\* **Purpose:** defines which accounts should have (or had) **BudgetPayment** rows for that template or month

\* **Template** — master list for recurring monthly planning (e.g. default household accounts)

\* **Budget** — snapshot for that month; may match the template or be customized after import/editing



\### Account list on budget create



When a budget is created, use the **AccountIds** from the selected **BudgetTemplate** (after validation). Do not accept a different list on create—the template is the source of truth for which accounts get **BudgetPayment** rows.



\* Copy the template's `AccountIds` onto the new **Budget** row when the budget is saved.

\* If the template's `AccountIds` is null or empty, creation fails with a clear error.

\* Persist the copied list on the **Budget** row so later template changes do not alter an existing month's membership.



\### Budget update



\* `PUT /api/v1/budgets/{id}` may include `accountIds` to replace the month's membership.

\* Adding an account id should create a **BudgetPayment** for that account if one does not exist (defaults: Pending, `PaymentMade` from account monthly payment or 0, `PaymentDate` = budget `StartPeriod`).

\* Removing an account id: prefer **soft handling** (document only for now)—either block removal when payments exist, or remove only ids with no payments; implementation to follow product choice.



\### Validation



\* Every id in `accountIds` must reference an existing **Account**.

\* Ids are unique within the list.

\* Order is preserved for display; duplicates are rejected.



\### Future schema note



A junction table (`BudgetAccount`, `BudgetTemplateAccount`) is preferable long term. Until then, `AccountIds` text matches the existing template pattern.



Each budget:



\* Has a month/year

\* Is created from a budget template

\* Stores its own `AccountIds` (see above)

\* On create, one **BudgetPayment** row is created per account id from the template's `AccountIds` (status **Pending**; `PaymentMade` defaults to the account's monthly payment or 0; `PaymentDate` defaults to the budget `StartPeriod`)

\* Contains multiple accounts/payments

\* Tracks total budgeted

\* Tracks total spent

\* Tracks remaining balance

\* A source of income can be selected for a budget

\* A source of income can be selected for a budget item

\* If a source of income is selected for specific budget item, the income amount should be deducted if the item is planned or paid.





\---



\# Dashboard Rules



Dashboard should display:



\* Total monthly bills

\* Total paid

\* Total overdue

\* Upcoming payments

\* Remaining balance

\* Budget utilization %

\*Income totals



\---



\# Account Balance Rules



Payments reduce balances.



Balances should:



\* Recalculate after payment changes, can be edited as well



\---



\# Validation Rules



\## Required Fields



Accounts require:



\* Name

\* Payment amount

\* Due date



Payments require:



\* Amount

\* Payment date

\* Account reference



Budgets require:



\* Month

\* Year

\* A budget template with a non-empty `AccountIds` list (copied to the budget on create)



\---



\# Audit Rules



All entities should track:



\* CreatedOn

\* CreatedBy

\* UpdatedOn

\* UpdatedBy



Dates should use UTC.



\---



\# Deletion Rules



Soft delete preferred initially.



Deleted records should:



\* Remain recoverable

\* Be excluded from standard queries



\---



\# Future AI Features



Potential future AI functionality:



\* Smart payment prioritization

\* Overspending alerts

\* Spending pattern detection

\* Financial forecasting

\* Savings recommendations





---

# Migration Ownership

See also **[database.md](./database.md)** for the table inventory, tenancy model, code paths, and migration workflow.

All Entity Framework Core migrations are centrally managed through:

```text
backend/src/CLS.Budget.Migration
```

Feature/domain projects must NOT contain their own migrations.

All schema changes must be added through the centralized migration project to maintain consistency and deployment control.
