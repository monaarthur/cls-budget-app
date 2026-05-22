# \# CLS Budget App — API Standards



\# API Style



Use RESTful APIs.

Use MVC controllers with attribute routing; do not use minimal APIs for CLS Budget endpoints

Implement swagger for documentation



Use JSON request/response bodies.



\# Endpoint Standards

Base route: api

Standardize on /api/v1/



Every path in this document is shown relative to /api/v1 unless marked otherwise



\## Accounts



GET /api/v1/accounts

GET /api/v1/accounts/{id}

POST /api/v1/accounts

PUT /api/v1/accounts/{id}

DELETE /api/v1/accounts/{id}



\---



\## Payments



GET /api/v1/payments

GET /api/v1/payments/{id}

POST /api/v1/payments

PUT /api/v1/payments/{id}

DELETE /api/v1/payments/{id}



Payment request/response bodies include `budgetPaymentStatusId` and `budgetPaymentStatusName` (not `isPaid`).



\---



\## Budget payment statuses



GET /api/v1/budget-payment-statuses



\---



\## Budgets



GET /api/v1/budgets

GET /api/v1/budgets/month/{month}/year/{year}

GET /api/v1/budgets/{id}

POST /api/v1/budgets

PUT /api/v1/budgets/{id}

DELETE /api/v1/budgets/{id}



Budget request/response bodies include `accountIds` (same semantics as **BudgetTemplate**):



\* **Type:** `number[]` in JSON on read; stored as JSON text on the `Budget` row, e.g. `[1,2,37]`

\* **Create:** do not send `accountIds`; server copies from the template's `accountIds` and saves on the budget

\* **Update:** optional `accountIds`; when provided, replaces the stored list (see domain-rules for payment sync rules)



Example **POST /api/v1/budgets** body:



```json
{
  "name": "May 2026 Budget",
  "startPeriod": "2026-05-01T00:00:00Z",
  "endPeriod": "2026-05-31T00:00:00Z",
  "budgetTemplateId": 1
}
```



Example **GET** budget response (additional field):



```json
{
  "budgetId": 1,
  "name": "May 2026 Budget",
  "startPeriod": "2026-05-01T00:00:00Z",
  "endPeriod": "2026-05-31T00:00:00Z",
  "budgetTemplateId": 1,
  "accountIds": [1, 2, 5, 12, 37]
}
```



\## Budget templates (reference)



Templates use the same `accountIds` field on `BudgetTemplates` (not yet exposed via a dedicated API controller; configured in DB/seed/import). Budget create always copies template `accountIds` onto the new budget row.



\---



\## CreditCard



GET /api/v1/creditcards

GET /api/v1/creditcards/{id}

POST /api/v1/creditcards

PUT /api/v1/creditcards/{id}

DELETE /api/v1/creditcards/{id}





\# HTTP Status Codes



Use proper status codes.



Examples:



200 OK

201 Created

204 No Content

400 Bad Request

401 Unauthorized

403 Forbidden

404 Not Found

500 Internal Server Error



\---



\# Request Standards



POST:



\* Create new resources



PUT:



\* Full updates



PATCH:



\* Partial updates if implemented later



DELETE:



\* Soft delete preferred



\---



\# API Response Standards



Use consistent response structures.



Example success response:



```json

{

\\\&#x20; "success": true,

\\\&#x20; "data": {},

\\\&#x20; "errors": \\\\\\\[]

}

```



Example error response:



```json

{

\\\&#x20; "success": false,

\\\&#x20; "data": null,

\\\&#x20; "errors": \\\\\\\[

\\\&#x20;   "Account not found."

\\\&#x20; ]

}

```



\---



\# Validation Standards



Use FluentValidation or equivalent.



Validation belongs in:



\* Application layer

\* Request validators



Do not rely solely on frontend validation.



\---



\# Versioning



Initial version:



```text

/api/v1

```



Future versions:



```text

/api/v2

```



\---



\# Swagger Standards



Swagger must be enabled in development.



Every endpoint should contain:



\* Summary

\* Request schema

\* Response schema



\---



\# Authentication



Future authentication:



\* Okta OAuth

\* JWT Bearer tokens



Do not hardcode secrets.



Use:



\* appsettings.Development.json

\* Environment variables

\* Secret Manager



\---



\# Logging Standards



Use structured logging.



Log:



\* Errors

\* Warnings

\* Important business events



Avoid logging:



\* Passwords

\* Sensitive financial data



\---



\# Pagination Standards



Large collections should support:



\* pageNumber

\* pageSize



Example:



```text

GET /api/accounts?pageNumber=1\\\\\\\&pageSize=20

```



\---



\# Sorting Standards



Support sorting when possible.



Example:



```text

GET /api/payments?sortBy=dueDate

```



\---



\# Filtering Standards



Support filtering for:



\* Status

\* Date range

\* Account type

\* Budget month/year



\---



\# Date Standards



Use UTC dates internally.



Frontend handles local display conversion.



ISO 8601 format preferred.



Example:



```text

2026-05-11T15:30:00Z

```



\---



\# CORS Standards



Allow frontend localhost access during development.



Example:



\* https://localhost:3000

\* http://localhost:5173



Restrict production origins later.



\---

# Initial API Implementation Priority

The Accounts domain is the first API module to be implemented end-to-end and serves as the reference implementation for future modules.

All subsequent modules should follow the same standards established by the Accounts implementation.

The Accounts implementation should establish the baseline patterns for:

* Controller structure
* DTO naming conventions
* Validation
* Service layer organization
* Repository usage
* Error handling
* Logging
* Dependency injection
* Entity Framework patterns
* PostgreSQL persistence
* Unit testing

