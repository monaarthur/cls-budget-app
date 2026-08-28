# CLS Budget App - System Documentation

## 1. Overview

CLS Budget App is a personal and household budget management platform with a web-based frontend and a layered backend API. The system supports account management, transactions, budgets, payments, income tracking, credit card planning, and administrative operations.

The application is structured as a separated frontend/backend solution:

- Frontend: Next.js with React and TypeScript
- Backend: ASP.NET Core Web API with Clean Architecture
- Data store: PostgreSQL via EF Core
- Deployment: container-friendly and environment-configurable

---

## 2. Purpose and Business Scope

The application helps users manage financial data by providing:

- Account and category tracking
- Budget creation and monitoring
- Income and expense processing
- Payment scheduling and payoff planning
- Credit card decision support
- Transaction import and data ingestion
- Admin and authentication workflows

---

## 3. High-Level Architecture

The system follows a modular, layered architecture:

### Frontend
- Built with Next.js
- Uses React and TypeScript
- Organizes features by business domain such as accounts, budgets, payments, transactions, and credit cards
- Uses app-based routing and shared UI components

### Backend
- ASP.NET Core Web API
- Structured into multiple projects for API, application logic, domain rules, infrastructure, import, and migrations
- Uses dependency injection, controllers, services, and repositories
- Implements authentication and authorization policies

### Data Layer
- EF Core for persistence
- PostgreSQL as the primary database engine
- Migration and import tooling included

---

## 4. Repository Structure

```text
cls-budget-app/
├── backend/
│   ├── src/
│   │   ├── CLS.Budget.Api/
│   │   ├── CLS.Budget.Application/
│   │   ├── CLS.Budget.Domain/
│   │   ├── CLS.Budget.Infrastructure/
│   │   ├── CLS.Budget.Import/
│   │   └── CLS.Budget.Migration/
│   ├── tests/
│   └── CLS.Budget.sln
├── data/
├── docs/
├── frontend/
│   └── cls-budget-web/
│       ├── app/
│       ├── public/
│       ├── src/
│       └── package.json
├── scripts/
└── versionsed-docs/
```

---

## 5. Frontend System Design

### Technology Stack
- Next.js 16
- React 19
- TypeScript
- Tailwind CSS
- ESLint

### Frontend Responsibilities
- Render user interfaces for budgeting features
- Handle form input and validation
- Communicate with backend APIs
- Present dashboards, summaries, and transaction information
- Support authentication-related flows such as login, registration, password reset, and forgot password

### Frontend Structure
The frontend is organized around app routes and feature-based folders:

- app/accounts
- app/admin
- app/budgets
- app/credit-cards
- app/income
- app/payments
- app/transactions
- app/login
- app/register
- app/forgot-password
- app/reset-password

This structure supports domain-driven UI development and makes it easier to extend individual financial features.

### Main Frontend Entry Points
- app/layout.tsx: root layout
- app/page.tsx: landing page
- middleware.ts: request middleware
- globals.css: global styles

---

## 6. Backend System Design

### Technology Stack
- ASP.NET Core Web API
- .NET
- EF Core
- FluentValidation
- JWT-based authentication support
- Swagger/OpenAPI for API exploration

### Backend Project Responsibilities

#### CLS.Budget.Api
- Hosts the HTTP API
- Configures services, authentication, CORS, and Swagger
- Maps controllers and exposes endpoints

#### CLS.Budget.Application
- Contains use cases and application services
- Holds business logic that is independent of infrastructure concerns

#### CLS.Budget.Domain
- Contains core entities, enums, and domain rules
- Keeps business rules free from external dependencies

#### CLS.Budget.Infrastructure
- Implements persistence, repositories, and infrastructure integrations
- Connects domain/application layers to databases and external systems

#### CLS.Budget.Import
- Supports data import workflow for transactions and related records

#### CLS.Budget.Migration
- Used for database migrations and migration tooling

### API Layer Notes
The API layer includes controllers for:
- Accounts
- Budgets
- Payments
- Transactions
- Credit cards
- Incomes
- Forecasting
- Auth and admin flows

---

## 7. Authentication and Authorization

The backend uses configurable authentication and authorization mechanisms.

### Current Design Notes
- Authentication can be enabled or disabled based on environment configuration
- JWT validation is supported when authentication is enabled
- Development-only authentication fallback exists for local testing
- Authorization policies enforce authenticated tenant users and role-based access

### Key Concepts
- Tenant-based access control
- Role-based authorization
- Claims-based identity integration

---

## 8. Core Domain Areas

The application covers several financial domains:

### Accounts
- Manage financial accounts and related metadata

### Budgets
- Create and maintain budgets
- Track spending against planned targets

### Payments
- Handle payment schedules and payment flow

### Transactions
- Record incomes and expenses
- Support import and review workflows

### Credit Cards
- Manage credit card records and related decision support features

### Income
- Track income sources and income entries

### Forecasting
- Support financial planning and future balance projections

---

## 9. Data and Persistence

The application uses EF Core with PostgreSQL for transactional persistence.

### Persistence Responsibilities
- Store accounts, budgets, transactions, payments, and related entities
- Apply migrations via the migration project
- Preserve data integrity through domain/application validation

### Data Assets
The repository also contains:
- data/ for persisted or seed-related resources
- docs/ with architecture and operational guidance
- scripts/ for support tasks and automation

---

## 10. Runtime Flow

A typical request flow looks like this:

1. A user interacts with the Next.js frontend.
2. The frontend calls a backend API endpoint.
3. The ASP.NET API controller receives the request.
4. The application layer performs validation and business logic.
5. Infrastructure services interact with the database.
6. The response is returned to the frontend and rendered to the user.

This flow preserves separation of responsibilities across layers.

---

## 11. Development Workflow

### Frontend
Run the frontend locally with:

```bash
cd frontend/cls-budget-web
npm install
npm run dev
```

### Backend
Run the backend locally with:

```bash
cd backend
dotnet restore
dotnet run --project src/CLS.Budget.Api/CLS.Budget.Api.csproj
```

### Database
Database setup depends on the configured PostgreSQL environment and migration tooling in the backend solution.

---

## 12. Deployment and Operations

The application is designed for a modern cloud or server deployment model:

- Backend API deployed as an ASP.NET service
- Frontend deployed as a Next.js web app
- Database hosted separately using PostgreSQL
- Environment-based configuration for development, production, and local settings

Operational documents in the repo include guidance for deployment, authentication, and environment configuration.

---

## 13. Documentation References

Additional repository documentation can be found in:

- docs/architecture.md
- docs/ADMIN.md
- docs/AUTH.md
- docs/DEPLOYMENT.md
- docs/database.md

---

## 14. Summary

CLS Budget App is a full-stack financial management application with a modern frontend and a layered .NET backend. Its architecture is designed for separation of concerns, maintainability, and future extension into additional budgeting, forecasting, and finance-management capabilities.
