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



GET /api/accounts

GET /api/accounts/{id}

POST /api/accounts

PUT /api/accounts/{id}

DELETE /api/accounts/{id}



\---



\## Payments



GET /api/payments

GET /api/payments/{id}

POST /api/payments

PUT /api/payments/{id}

DELETE /api/payments/{id}



\---



\## Budgets



GET /api/budgets

GET /api/budgets/month/{month}/year/{year}

GET /api/budgets/{id}

POST /api/budgets

PUT /api/budgets/{id}

DELETE /api/budgets/{id}



\---



\## Payments



GET /api/creditcards

GET /api/creditcards/{id}

POST /api/creditcards

PUT /api/creditcards/{id}

DELETE /api/creditcards/{id}





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

\&#x20; "success": true,

\&#x20; "data": {},

\&#x20; "errors": \\\[]

}

```



Example error response:



```json

{

\&#x20; "success": false,

\&#x20; "data": null,

\&#x20; "errors": \\\[

\&#x20;   "Account not found."

\&#x20; ]

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

GET /api/accounts?pageNumber=1\\\&pageSize=20

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

