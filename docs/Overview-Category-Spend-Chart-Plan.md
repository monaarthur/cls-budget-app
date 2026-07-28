# Implementation Plan: Overview Category Spend Chart

**Status:** On hold — review later, do not implement yet  
**Created:** 2026-07-28  
**Source:** Chat request (Overview graph: spending by category per month and over the year)

---

## Overview

Add a chart on the Overview page (`DashboardHome`) that shows **spending by account category for each month of the current year**, plus a clear **year total** view. This builds on the existing YTD-by-category bars already on Overview.

---

## Technical approach

| Decision | Recommendation | Why |
|----------|----------------|-----|
| Data source | `BudgetPayment.paymentMade` × `Account.accountCategoryId` | Same as current YTD section; dates via `clearedDate \|\| paymentDate` |
| Scope | Current calendar year, Jan–Dec (future months = 0) | Matches “over the year” |
| Chart type | **Stacked bar**: X = month, stack = category | Best for month × category |
| Year rollup | Keep/enhance existing YTD list; chart shows monthly composition | Avoid two competing year charts |
| Aggregation | Client-side first (extend `ytdSpendingByCategory` utils) | Overview already loads all payments/accounts |
| Chart library | Add **Recharts** | No chart lib today; Recharts fits React/Next well |
| Backend API | Optional later phase | Only if payment volume makes client aggregation slow |

### Proposed data shape

```ts
type MonthlyCategorySpend = {
  year: number;
  months: {
    month: number; // 1–12
    label: string; // "Jan"
    byCategory: {
      accountCategoryId: number;
      categoryName: string;
      totalPaid: number;
    }[];
    totalPaid: number;
  }[];
  categories: {
    accountCategoryId: number;
    categoryName: string;
    yearTotal: number;
  }[];
  yearTotal: number;
};
```

### UI placement

New section on Overview under the existing “YYYY spending by category” list (or replace the simple bars with chart + compact legend/table).

### Relevant existing code

- `frontend/cls-budget-web/src/features/dashboard/components/DashboardHome.tsx`
- `frontend/cls-budget-web/src/features/dashboard/hooks/useDashboardSummary.ts`
- `frontend/cls-budget-web/src/features/dashboard/utils/ytdSpendingByCategory.ts`

---

## Phases

### Phase 1 — Data model & aggregation

- [ ] Extract shared spend-date helpers from `ytdSpendingByCategory.ts`
- [ ] Add `computeMonthlySpendingByCategory(payments, accounts, year?)`
- [ ] Cover edge cases: empty year, missing category, zero `paymentMade`, missing dates
- [ ] Wire result into `useDashboardSummary` as `monthlySpendingByCategory`

### Phase 2 — Chart dependency & component

- [ ] Add `recharts` to `cls-budget-web`
- [ ] Build `CategorySpendChart` (stacked bars, currency tooltips, legend, empty state)
- [ ] Responsive layout (mobile: horizontal scroll or fewer ticks)
- [ ] Category colors: stable palette keyed by `accountCategoryId`

### Phase 3 — Overview integration

- [ ] Render chart section on `DashboardHome` with year label + YTD total
- [ ] Align with existing YTD list (same totals; list can stay as detail)
- [ ] Loading/skeleton + empty copy when no paid amounts YTD
- [ ] Optional toggle “Stacked” vs “Grouped” — out of scope for v1

### Phase 4 — Polish & verify

- [ ] Confirm totals match sum of monthly stacks and existing YTD rows
- [ ] Accessibility: legend text, tooltip amounts, not color-only
- [ ] Manual check on Overview with multi-category / multi-month data

### Phase 5 (optional) — Backend endpoint

- [ ] `GET /api/v1/dashboard/spending-by-category?year=2026`
- [ ] SQL group by month + category; return same DTO shape
- [ ] Switch Overview hook to API if client payload gets heavy

---

## Dependencies

- Existing Overview payment/account loads (`useDashboardSummary`)
- Account categories populated (`accountCategoryId` / name on accounts)
- Payments with `paymentMade > 0` and a usable date
- Grace period / grace day work is unrelated — no blocker

---

## Risks

| Risk | Mitigation |
|------|------------|
| Sparse months look empty | Show all 12 months; zero-height stacks OK |
| Many categories clutter legend | Cap series to top N + “Other”, or reuse YTD sort order |
| Day-of-month wrap ≠ real calendar months | Chart uses actual payment dates’ calendar month, not grace day |
| Recharts bundle size | Dynamic import chart component on Overview only |
| Client aggregation at scale | Phase 5 API |

---

## Acceptance criteria

1. Overview shows a chart of category spend **per month** for the current year
2. Stacks/series are account categories; tooltips show category + amount
3. Year total on the section matches summed monthly category totals and aligns with existing YTD list
4. Empty/loading states are clear
5. Works on desktop and mobile without breaking Overview layout
