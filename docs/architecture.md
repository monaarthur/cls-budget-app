# CLS Budget App — Architecture Standards

## Overview

CLS Budget App uses a separate frontend/backend architecture.

Frontend:

* React and Vite
* TypeScript
* Tailwind CSS

Backend:

* ASP.NET Core Web API (.NET 9)
* EF Core 9
* PostgreSQL

Architecture Pattern:

* Clean Architecture

\---

# Architecture Principles

## 1\. Separation of Concerns

Responsibilities must remain separated between layers.

Frontend:

* UI
* User interactions
* API communication
* Form validation
* State management

Backend:

* Business rules
* Validation
* Data persistence
* Security
* Calculations

Database:

* Storage only

\---

## 2\. Thin Controllers

Controllers should:

* Receive requests
* Validate DTOs
* Call services
* Return responses

Controllers should NOT:

* Contain business rules
* Contain EF Core queries
* Contain calculations
* Should not use minimal apis

\---

## 3\. Application Layer Owns Business Logic

Business logic belongs in:

* Application services
* Domain entities
* Domain rules

Examples:

* Overdue logic
* Interest calculations
* Payment recommendation logic
* Budget summaries
* Resolving budget `accountIds` from request, budget row, or template on create

\---

## 4\. Domain Layer Rules

The Domain layer contains:

* Entities
* Enums
* Value objects
* Domain rules

The Domain layer must NOT reference:

* EF Core
* ASP.NET
* Infrastructure libraries

\---

## 5\. Infrastructure Layer Rules

Infrastructure handles:

* EF Core
* PostgreSQL
* Repositories
* External services
* File storage
* Logging integrations

Infrastructure must not contain business rules.

\---

## 6\. DTO Standards

Never expose EF entities directly through API responses.

Use DTOs for:

* Requests
* Responses
* Updates

Example:

* CreateAccountRequest
* AccountResponse
* UpdatePaymentRequest

\---

## 7\. Async Standards

Use async/await everywhere possible.

All database operations should be async.

Example:

* ToListAsync()
* SaveChangesAsync()

\---

## 8\. Repository Pattern

Repositories should:

* Abstract database access
* Be interface driven
* Avoid business logic

Business rules belong in services.

\---

## 9\. Frontend Architecture

Frontend should be feature-based.

Example:

features/

* accounts/
* payments/
* budgets/
* dashboard/

Each feature should contain:

* Components
* Hooks
* API calls
* Types
* Validation

\---

## 10\. API Standards

Use RESTful endpoints.

Examples:

GET /api/v1/accounts
GET /api/v1/accounts/{id}
POST /api/v1/accounts
PUT /api/v1/accounts/{id}
DELETE /api/v1/accounts/{id}

\---

## 11\. Naming Conventions

Classes:

* PascalCase

Variables:

* camelCase

Interfaces:

* Prefix with I

Async methods:

* Suffix Async

Examples:

* IAccountRepository
* GetAccountsAsync()

\---

## 12\. Testing Standards

Use:

* xUnit
* Moq



Required tests:

* Services
* Business rules
* Validation
* Edge cases

Avoid testing EF Core implementation details directly.

\---

## 13\. Dependency Injection

Use Microsoft Dependency Injection.

Register:

* Services
* Repositories
* DbContexts
* External providers

Program.cs should remain clean and organized.

\---

## 14\. Error Handling

Use centralized exception middleware.

Avoid try/catch blocks in every controller.

Return standardized API responses.

\---

## 15\. Security Standards

Never trust frontend validation alone.

Validate:

* Input
* Authentication
* Authorization
* Ownership

Sensitive configuration must use environment variables.



\---

## 16\. Implementation Order / First Vertical Slice

The first end-to-end vertical slice for the system is:

* Accounts CRUD

This slice includes:

* ASP.NET Core API endpoints
* Application services
* Repository layer
* Entity Framework Core integration
* PostgreSQL persistence
* Validation
* Dependency injection wiring
* Unit tests

The purpose of this vertical slice is to establish the baseline architecture, coding standards, repository patterns, DTO conventions, validation patterns, and persistence strategy before implementing additional domains.

Additional modules should follow the same architectural and implementation patterns established by the Accounts feature.

\---

## 17\. Entity Framework Migrations Strategy

`CLS.Budget.Migration` located under:

```text
backend/src/CLS.Budget.Migration
```

is the ONLY project responsible for managing Entity Framework Core migrations.

All migration commands must be executed against this project.

Example:

```bash
dotnet ef migrations add InitialCreate   --project src/CLS.Budget.Migration   --startup-project src/CLS.Budget.Api
```

```bash
dotnet ef database update   --project src/CLS.Budget.Migration   --startup-project src/CLS.Budget.Api
```

No other project should contain migrations.

No additional migration projects should be created unless explicitly approved through an architectural decision update.

