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

GET /api/accounts
GET /api/accounts/{id}
POST /api/accounts
PUT /api/accounts/{id}
DELETE /api/accounts/{id}

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

