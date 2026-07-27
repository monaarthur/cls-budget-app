# Credit Card Decision Engine — Feature Implementation Guide

## 1. Purpose

This document defines how to extend the existing .NET credit card application into a credit card decision engine.

The application already collects and stores credit card information. The next phase is to add calculation engines, recommendation logic, forecasting, scenario comparisons, and APIs that help users decide how to reduce debt, lower interest, improve utilization, and reach payoff goals.

The system should be implemented feature by feature so each capability can be built, tested, and released independently.

---

## 2. Existing Assumptions

The current application already contains:

- A .NET Web API application
- Credit card records
- User-entered balances
- Credit limits
- Annual percentage rates
- Minimum payments
- Payment due dates
- Entity Framework Core
- A relational database
- Authentication or a user identifier
- CRUD operations for credit card information

If any of these items do not yet exist, add them before implementing the decision engines.



## 3. CreditCardEngine API Updates

Contains:

- Controllers 
- Request and response models
- API validation


#### CreditCardEngine Application Updates

Contains:

- Use cases
- Commands and queries
- Application services
- Feature orchestration
- DTO mapping
- Interfaces for infrastructure dependencies

#### CreditCardEngine Domain

Contains:

- Credit card entities
- Value objects
- Domain services
- Calculation rules
- Payoff strategy logic
- Recommendation rules
- Domain exceptions


## 3. CreditCardEngine Worker Updates

#### CreditCardEngine.Worker

Contains:

- Scheduled forecast generation
- Monthly snapshots
- Recommendation refresh jobs
- Notification processing

---

## 4. Core Domain Model

Use the application's existing credit card entity where possible. Add only missing fields.

```csharp
public sealed class CreditCard
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal CurrentBalance { get; set; }

    public decimal CreditLimit { get; set; }

    public decimal AnnualPercentageRate { get; set; }

    public decimal MinimumPayment { get; set; }

    public decimal? MinimumPaymentPercentage { get; set; }

    public decimal? MinimumPaymentFloor { get; set; }

    public DateOnly? PaymentDueDate { get; set; }

    public decimal? PromotionalAnnualPercentageRate { get; set; }

    public DateOnly? PromotionalRateExpirationDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedOnUtc { get; set; }

    public DateTime UpdatedOnUtc { get; set; }
}
```

### Derived Values

Do not persist derived values unless required for reporting.

```csharp
public decimal AvailableCredit =>
    Math.Max(0, CreditLimit - CurrentBalance);

public decimal UtilizationPercentage =>
    CreditLimit <= 0
        ? 0
        : CurrentBalance / CreditLimit * 100;
```

---

## 5. Shared Calculation Rules

Create reusable value objects and helpers for monetary calculations.

### Money Rules

- Use `decimal` for all currency calculations.
- Round displayed currency to two decimal places.
- Preserve additional precision during intermediate calculations.
- Never use `double` for balances, interest, fees, or payments.
- Reject negative balances unless the application explicitly supports credit balances.
- Reject APR values below zero.
- Reject credit limits less than zero.
- Ensure payment amounts are greater than zero.

### APR Conversion

```csharp
decimal MonthlyRate(decimal annualPercentageRate)
{
    return annualPercentageRate / 100m / 12m;
}

decimal DailyRate(decimal annualPercentageRate)
{
    return annualPercentageRate / 100m / 365m;
}
```

### Minimum Payment Rule

Support configurable minimum payment rules.

```csharp
decimal CalculateMinimumPayment(
    decimal balance,
    decimal percentage,
    decimal floor)
{
    if (balance <= 0)
    {
        return 0;
    }

    var percentageAmount = balance * percentage;

    return Math.Min(
        balance,
        Math.Max(percentageAmount, floor));
}
```

---

# Feature 1 — Interest Calculation Engine

## Goal

Calculate current and projected credit card interest.

## Required Inputs

- Current balance
- APR
- Optional promotional APR
- Promotional expiration date
- Payment amount
- Calculation start date
- Optional transaction activity

## Required Outputs

- Estimated daily interest
- Estimated monthly interest
- Estimated annual interest
- Projected total interest
- Principal paid
- Interest paid
- Remaining balance
- Estimated payoff date

## Interface

```csharp
public interface IInterestCalculationEngine
{
    InterestCalculationResult Calculate(
        InterestCalculationRequest request);
}
```

## Request Model

```csharp
public sealed record InterestCalculationRequest(
    decimal Balance,
    decimal AnnualPercentageRate,
    decimal MonthlyPayment,
    DateOnly StartDate,
    decimal? PromotionalAnnualPercentageRate = null,
    DateOnly? PromotionalRateExpirationDate = null);
```

## Result Model

```csharp
public sealed record InterestCalculationResult(
    decimal DailyInterest,
    decimal EstimatedMonthlyInterest,
    decimal EstimatedAnnualInterest,
    decimal TotalInterestPaid,
    decimal TotalPrincipalPaid,
    decimal RemainingBalance,
    DateOnly? EstimatedPayoffDate,
    int NumberOfPayments,
    bool NegativeAmortizationDetected);
```

## Implementation Rules

1. Determine the applicable APR for each period.
2. Apply promotional APR before the expiration date.
3. Apply standard APR after promotion expiration.
4. Calculate interest before applying the monthly payment.
5. Stop when the balance reaches zero.
6. Add a maximum iteration limit, such as 1,200 months.
7. Mark the result as negative amortization when the payment does not cover monthly interest.
8. Return a null payoff date when payoff cannot be reached.

## API Endpoint

```http
POST /api/v1/credit-cards/{creditCardId}/interest-analysis
```

## Acceptance Criteria

- Returns accurate interest for zero-percent promotional periods.
- Uses the regular APR after the promotion expires.
- Detects payments that are too low to reduce principal.
- Does not loop indefinitely.
- Has unit tests for zero balance, zero APR, high APR, and negative amortization.

---

# Feature 2 — Payoff Strategy Engine

## Goal

Determine the order in which cards should be paid and generate a monthly payoff plan.

## Supported Strategies

- Avalanche
- Snowball
- Hybrid
- Custom order
- Utilization-first

## Strategy Definitions

### Avalanche

Pay the minimum on every card and apply extra funds to the card with the highest effective APR.

### Snowball

Pay the minimum on every card and apply extra funds to the card with the smallest current balance.

### Hybrid

Use a weighted score based on APR, balance, and utilization.

Example:

```text
Hybrid Score =
    APR Weight
  + Utilization Weight
  + Small Balance Weight
```

### Utilization-First

Prioritize cards that can be moved below important utilization thresholds, such as:

- 90%
- 70%
- 50%
- 30%
- 10%

## Interface

```csharp
public interface IPayoffStrategyEngine
{
    PayoffPlanResult GeneratePlan(
        PayoffPlanRequest request);
}
```

## Request Model

```csharp
public sealed record PayoffPlanRequest(
    IReadOnlyCollection<CreditCardPayoffInput> CreditCards,
    decimal TotalMonthlyDebtPayment,
    PayoffStrategyType Strategy,
    DateOnly StartDate);
```

## Output

The result should contain:

- Recommended card order
- Monthly payment allocation
- Estimated payoff date per card
- Overall debt-free date
- Total interest paid
- Total interest saved compared with minimum payments
- Number of months to payoff
- Monthly payoff schedule

## Monthly Schedule Model

```csharp
public sealed record MonthlyPayoffScheduleItem(
    DateOnly Month,
    Guid CreditCardId,
    decimal StartingBalance,
    decimal InterestCharged,
    decimal PaymentApplied,
    decimal PrincipalApplied,
    decimal EndingBalance);
```

## Rules

1. Always calculate required minimum payments first.
2. Reject plans where the total monthly debt payment is below the combined minimum payment.
3. Apply remaining extra funds according to the selected strategy.
4. Roll freed payments into the next target card after a card is paid off.
5. Recalculate minimum payments monthly when percentage-based minimums are used.
6. Stop when all balances reach zero.
7. Protect against infinite loops.

## API Endpoint

```http
POST /api/v1/payoff-plans
```

## Acceptance Criteria

- Avalanche prioritizes the highest APR.
- Snowball prioritizes the lowest balance.
- Extra funds roll to the next card.
- Total principal paid equals the starting debt.
- The monthly schedule reconciles exactly to the final balances.
- Results are deterministic for identical inputs.

---

# Feature 3 — Utilization Engine

## Goal

Calculate individual and overall revolving credit utilization and determine payment targets.

## Interface

```csharp
public interface IUtilizationEngine
{
    UtilizationResult Calculate(
        IReadOnlyCollection<CreditCardUtilizationInput> cards);
}
```

## Required Outputs

- Utilization percentage per card
- Overall utilization
- Total balances
- Total credit limits
- Amount required to reach each threshold
- Recommended card payment order for utilization improvement

## Threshold Calculations

For each card:

```text
Target Balance = Credit Limit × Target Utilization Percentage
Payment Required = Current Balance - Target Balance
```

Use zero when the calculated payment required is negative.

## Example Thresholds

```csharp
public static readonly decimal[] DefaultThresholds =
{
    90m,
    70m,
    50m,
    30m,
    10m
};
```

## API Endpoint

```http
GET /api/v1/credit-cards/utilization-summary
```

## Acceptance Criteria

- Handles cards with zero limits safely.
- Calculates both per-card and overall utilization.
- Returns payment amounts needed to reach each target.
- Does not claim a guaranteed credit-score increase.
- Labels credit-score effects as estimates only.

---

# Feature 4 — Balance Transfer Engine

## Goal

Determine whether transferring debt to another card or offer is financially beneficial.

## Inputs

- Transfer amount
- Current APR
- New promotional APR
- Promotional period in months
- Transfer fee percentage
- Transfer fee flat amount
- New regular APR
- Planned monthly payment
- Available transfer limit

## Interface

```csharp
public interface IBalanceTransferEngine
{
    BalanceTransferAnalysisResult Analyze(
        BalanceTransferAnalysisRequest request);
}
```

## Required Outputs

- Total transfer fee
- Interest without transfer
- Interest with transfer
- Net savings
- Break-even month
- Balance remaining when promotion ends
- Payment needed to clear the balance before promotion expires
- Recommendation status
- Warning messages

## Recommendation Status

```csharp
public enum BalanceTransferRecommendation
{
    Recommended,
    PotentiallyBeneficial,
    NotRecommended,
    InsufficientInformation
}
```

## Rules

1. Do not transfer more than the available transfer limit.
2. Include the transfer fee in the new balance unless the offer handles it separately.
3. Compare both scenarios over the same time period.
4. Consider the regular APR after the promotional period.
5. Warn when the planned payment will not clear the transferred balance before expiration.
6. Warn when the transfer fee exceeds estimated interest savings.
7. Do not present the result as financial advice.

## API Endpoint

```http
POST /api/v1/balance-transfers/analyze
```

## Acceptance Criteria

- Correctly adds percentage and flat fees.
- Calculates the payment required to finish before expiration.
- Handles zero-percent offers.
- Compares equivalent time horizons.
- Returns a clear explanation of the recommendation.

---

# Feature 5 — Cash Flow Affordability Engine

## Goal

Determine how much the user can safely allocate to debt payments.

## Inputs

- Monthly net income
- Required expenses
- Variable expenses
- Existing debt minimums
- Emergency savings contribution
- User-selected safety buffer
- Additional available funds

## Interface

```csharp
public interface ICashFlowEngine
{
    CashFlowResult Calculate(
        CashFlowRequest request);
}
```

## Required Outputs

- Monthly disposable income
- Required debt minimums
- Safe extra debt payment
- Aggressive extra debt payment
- Remaining cash buffer
- Affordability warnings

## Rules

```text
Disposable Income =
    Net Income
  - Required Expenses
  - Variable Expenses
  - Minimum Debt Payments
  - Savings Contribution
```

Recommended extra payment:

```text
Safe Extra Payment =
    max(0, Disposable Income - Safety Buffer)
```

## API Endpoint

```http
POST /api/v1/cash-flow/analyze
```

## Acceptance Criteria

- Never recommends a negative payment.
- Clearly separates minimum payments from extra payments.
- Warns when expenses exceed income.
- Allows the user to override the recommended amount.

---

# Feature 6 — Forecast Engine

## Goal

Project debt balances, interest, utilization, and payoff progress over time.

## Inputs

- Credit cards
- Selected payoff strategy
- Monthly payment amount
- Income changes
- Expense changes
- Additional charges
- Promotional APR expiration
- One-time payments
- Forecast duration

## Interface

```csharp
public interface IForecastEngine
{
    ForecastResult Generate(
        ForecastRequest request);
}
```

## Required Outputs

For each forecast month:

- Starting debt
- New charges
- Interest
- Payments
- Ending debt
- Total credit limit
- Overall utilization
- Available cash
- Cards paid off
- Cumulative interest

## Persistence

Store generated forecasts only when the user saves a scenario.

Suggested tables:

```text
ForecastScenario
ForecastScenarioCreditCard
ForecastMonthlySnapshot
```

## API Endpoints

```http
POST /api/v1/forecasts
GET  /api/v1/forecasts/{forecastId}
DELETE /api/v1/forecasts/{forecastId}
```

## Acceptance Criteria

- Forecast balances match the payoff engine.
- Handles one-time and recurring payment changes.
- Handles promotional APR expiration.
- Supports at least 120 months.
- Returns warnings when debt increases over time.

---

# Feature 7 — Recommendation Engine

## Goal

Combine the outputs of the other engines into prioritized, explainable recommendations.

## Important Design Rule

The recommendation engine should not duplicate calculation logic.

It should consume results from:

- Interest engine
- Payoff engine
- Utilization engine
- Balance transfer engine
- Cash flow engine
- Forecast engine

## Interface

```csharp
public interface IRecommendationEngine
{
    RecommendationResult Generate(
        RecommendationContext context);
}
```

## Recommendation Model

```csharp
public sealed record Recommendation(
    string Code,
    string Title,
    string Explanation,
    RecommendationPriority Priority,
    decimal? EstimatedSavings,
    int? EstimatedMonthsSaved,
    Guid? CreditCardId,
    IReadOnlyCollection<string> Warnings);
```

## Example Recommendation Codes

```text
PAY_HIGHEST_APR_FIRST
PAY_CARD_BELOW_30_PERCENT
PAY_CARD_BELOW_10_PERCENT
INCREASE_MONTHLY_PAYMENT
TRANSFER_BALANCE
PROMOTIONAL_RATE_EXPIRING
PAYMENT_BELOW_MONTHLY_INTEREST
INSUFFICIENT_CASH_BUFFER
BUILD_EMERGENCY_RESERVE
AVOID_NEW_CHARGES
```

## Priority Levels

```csharp
public enum RecommendationPriority
{
    Critical,
    High,
    Medium,
    Low,
    Informational
}
```

## Explainability Requirements

Every recommendation must explain:

- What the engine detected
- Why it matters
- What action is suggested
- Estimated financial effect
- Assumptions
- Relevant warnings

## API Endpoint

```http
GET /api/v1/recommendations
```

## Acceptance Criteria

- Recommendations are deterministic.
- No recommendation is created without supporting calculation results.
- Conflicting recommendations are ranked or suppressed.
- Every recommendation includes a plain-language explanation.
- Financial outcomes are labeled as estimates.

---

# Feature 8 — Financial Decision Simulator

## Goal

Compare multiple user-defined financial decisions.

## Example Scenarios

- Apply a $5,000 bonus to the highest-APR card
- Pay off the smallest balances
- Keep $2,000 in savings and apply the remainder
- Use a balance transfer offer
- Increase monthly payments by $200
- Add a one-time payment
- Pause extra payments for three months

## Interface

```csharp
public interface IScenarioComparisonEngine
{
    ScenarioComparisonResult Compare(
        ScenarioComparisonRequest request);
}
```

## Required Outputs

For each scenario:

- Debt-free date
- Total interest
- Interest savings
- Months saved
- Ending emergency fund
- Lowest monthly cash balance
- Utilization after selected periods
- Risk warnings
- Recommended scenario

## API Endpoint

```http
POST /api/v1/scenarios/compare
```

## Acceptance Criteria

- Every scenario begins with the same baseline data.
- Results use the same forecast duration.
- The engine clearly identifies assumptions.
- The recommended scenario is based on configurable criteria.
- Users can view all scenarios even when one is recommended.

---

# Feature 9 — Financial Health Score

## Goal

Provide a simple score that summarizes debt risk and payoff progress.

## Warning

The score must be presented as an application-specific educational score, not an official credit score.

## Suggested Categories

```text
Payment Sustainability       25 points
Credit Utilization           25 points
Interest Cost                20 points
Payoff Progress              15 points
Emergency Reserve            10 points
Promotional Rate Risk         5 points
```

## Output

- Score from 0 to 100
- Category scores
- Reasons for deductions
- Improvement actions
- Score trend over time

## API Endpoint

```http
GET /api/v1/financial-health
```

## Acceptance Criteria

- The formula is versioned.
- Score calculations are reproducible.
- Category weights are configurable.
- The response explains every deduction.
- The application does not call it a FICO score or credit bureau score.

---

# Feature 10 — Monthly Snapshots and Background Processing

## Goal

Create monthly historical records and refresh forecasts and recommendations.

## Worker Responsibilities

- Create monthly account snapshots
- Recalculate utilization
- Refresh saved forecasts
- Detect promotional APR expiration
- Detect cards at risk of negative amortization
- Generate recommendation changes
- Create notification events

## Suggested Background Job

```csharp
public sealed class MonthlySnapshotWorker : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Find due snapshot jobs.
            // Create snapshots.
            // Refresh recommendations.
            // Save results.
            // Wait until the next scheduled run.
        }
    }
}
```

For production, use a durable scheduler such as:

- Hangfire
- Quartz.NET
- AWS EventBridge with Lambda
- ECS scheduled tasks

## Suggested Tables

```text
CreditCardMonthlySnapshot
RecommendationSnapshot
FinancialHealthScoreSnapshot
BackgroundJobExecution
```

---

# Feature 11 — Notifications

## Goal

Notify users about important financial events.

## Notification Types

- Payment due soon
- Promotional APR expiring
- Utilization crossed a threshold
- Card paid off
- Monthly interest increased
- Forecast changed materially
- Payment will not cover interest
- Recommended balance transfer nearing expiration

## Event Model

```csharp
public sealed record NotificationRequested(
    Guid UserId,
    string NotificationType,
    string Title,
    string Message,
    DateTime CreatedOnUtc);
```

## Architecture

Prefer event-driven processing.

```text
Domain Event
    ↓
Application Event Handler
    ↓
Message Queue
    ↓
Notification Worker
    ↓
Email, SMS, or In-App Notification
```

---

# 6. API Standards

## Base Route

```text
/api/v1
```

## Response Requirements

Every calculation response should include:

- Calculation date
- Input assumptions
- Results
- Warnings
- Formula version
- Correlation ID

Example:

```json
{
  "calculatedOnUtc": "2026-07-15T18:00:00Z",
  "formulaVersion": "1.0",
  "warnings": [],
  "data": {}
}
```

## Validation

Use FluentValidation or equivalent validation.

Validate:

- Missing balances
- Invalid APR values
- Credit limit below balance when not allowed
- Payment below zero
- Invalid promotional dates
- Transfer amount above transfer limit
- Forecast duration outside the allowed range

## Error Handling

Use centralized exception handling middleware.

Return RFC 7807 Problem Details.

```csharp
builder.Services.AddProblemDetails();
```

---

# 7. Database Additions

Suggested new entities:

```text
CreditCardPayment
PayoffPlan
PayoffPlanItem
BalanceTransferOffer
BalanceTransferAnalysis
ForecastScenario
ForecastMonthlySnapshot
Recommendation
RecommendationSnapshot
FinancialHealthScoreSnapshot
CreditCardMonthlySnapshot
Notification
```

## Important Persistence Rule

Calculation engines should remain stateless.

Only application services should decide whether results are saved.

---

# 8. Testing Strategy

## Unit Tests

Each engine must have independent unit tests.

Test categories:

- Normal calculations
- Zero balances
- Zero APR
- Very high APR
- Promotional APR transitions
- Payments below interest
- Payments equal to balance
- Multiple cards
- Extra payment rollovers
- Invalid inputs
- Rounding behavior
- Maximum forecast duration

## Integration Tests

Test:

- API endpoint validation
- Database persistence
- User data isolation
- Saved forecasts
- Recommendation generation
- EF Core mappings
- Background worker idempotency

## Golden Calculation Tests

Create fixed test cases with known expected results.

Example:

```text
Balance: $1,000
APR: 12%
Payment: $100
Expected first-month interest: approximately $10
Expected first-month principal: approximately $90
```

Store expected values so future formula changes can be detected.

---

# 9. Security Requirements

- Authorize every user-specific endpoint.
- Never accept a user ID directly from the client when it can be read from authentication claims.
- Filter every database query by the authenticated user.
- Do not log full account numbers.
- Store only the last four digits when an account identifier is needed.
- Encrypt sensitive data at rest.
- Use HTTPS.
- Apply rate limiting to calculation endpoints.
- Validate all inputs.
- Add audit logging for saved plans and recommendation changes.

---

# 10. Observability

Add structured logging for:

- Engine name
- Formula version
- User ID
- Scenario ID
- Credit card count
- Forecast duration
- Calculation duration
- Warning count
- Failure reason

Do not log:

- Full card numbers
- Authentication tokens
- Sensitive personal financial details unless masked

Add metrics for:

- Calculation request count
- Average calculation duration
- Failed calculations
- Negative amortization detections
- Saved forecast count
- Recommendation count
- Background job failures

---

# 11. Performance and Scalability

- Keep engines stateless.
- Use asynchronous database operations.
- Avoid loading unrelated user data.
- Cache dashboard summaries for short periods.
- Do not cache user-specific calculations without including the user and input version in the cache key.
- Add cancellation tokens to all API and application methods.
- Limit maximum forecast periods.
- Consider background processing for large scenario comparisons.
- Use database indexes on `UserId`, `CreditCardId`, `ScenarioId`, and snapshot dates.

---

# 12. Feature Build Order

Implement the features in this order.

## Phase 1 — Calculation Foundation

1. Shared money and APR helpers
2. Interest calculation engine
3. Utilization engine
4. Unit tests

## Phase 2 — Payoff Planning

5. Payoff strategy engine
6. Avalanche strategy
7. Snowball strategy
8. Monthly payoff schedule
9. Payoff API

## Phase 3 — Advanced Decisions

10. Balance transfer engine
11. Cash flow engine
12. Forecast engine
13. Saved forecast scenarios

## Phase 4 — Intelligence Layer

14. Recommendation engine
15. Financial decision simulator
16. Financial health score
17. Plain-language explanations

## Phase 5 — Automation

18. Monthly snapshots
19. Background jobs
20. Notifications
21. Recommendation history

---

# 13. Definition of Done for Every Feature

A feature is complete only when:

- Domain logic is implemented.
- Application service is implemented.
- API endpoint is implemented.
- Input validation is implemented.
- Unit tests are implemented.
- Integration tests are implemented.
- OpenAPI documentation is updated.
- Logging is added.
- Security is reviewed.
- Edge cases are tested.
- Results include assumptions and warnings.
- The feature is documented.

---

# 14. First Implementation Sprint

The first sprint should deliver a working debt analysis foundation.

## Sprint Scope

Build:

- Interest calculation engine
- Utilization engine
- Avalanche payoff engine
- Snowball payoff engine
- Payoff comparison endpoint

## Sprint Endpoint

```http
POST /api/v1/payoff-plans/compare
```

## Example Response

```json
{
  "startingDebt": 48500.00,
  "monthlyPayment": 1800.00,
  "strategies": [
    {
      "strategy": "Avalanche",
      "estimatedPayoffDate": "2029-08-01",
      "totalInterest": 11250.00,
      "monthsToPayoff": 37
    },
    {
      "strategy": "Snowball",
      "estimatedPayoffDate": "2029-11-01",
      "totalInterest": 12890.00,
      "monthsToPayoff": 40
    }
  ],
  "recommendedStrategy": "Avalanche",
  "reason": "The avalanche strategy is estimated to save $1,640 in interest and finish three months earlier.",
  "warnings": [
    "Results are estimates and assume no new charges."
  ]
}
```

---

# 15. Required Disclaimers

Display a disclaimer with financial calculations and recommendations.

```text
The calculations and recommendations provided by this application are estimates for educational and planning purposes only. They are not financial, legal, tax, or credit advice. Actual interest charges, credit-score effects, fees, and payoff dates may differ based on lender rules, transaction timing, and account activity.
```

---

# 16. Engineering Principles

Follow these rules throughout implementation:

- Keep calculations deterministic.
- Keep engines stateless.
- Separate calculations from persistence.
- Separate recommendations from formulas.
- Version calculation formulas.
- Return assumptions with every result.
- Prefer explainable rules over hidden logic.
- Use automated tests for all financial formulas.
- Never guarantee credit-score improvements.
- Never expose one user's financial information to another user.
- Build each feature so it can be replaced or enhanced independently.
