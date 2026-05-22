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



Valid statuses:



\* Pending

\* Scheduled

\* Paid

\* Failed

\* Overdue



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



Each budget:



\* Has a month/year

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



