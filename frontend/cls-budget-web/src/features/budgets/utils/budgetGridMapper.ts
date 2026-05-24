import type { AccountResponse } from "@/features/accounts/types/account";
import type { PaymentResponse, UpdatePaymentRequest } from "@/features/payments/types/payment";

export interface BudgetGridRow extends PaymentResponse {
  accountName: string;
  accountNumber: string;
  accountCategoryId: number;
  accountBalance: number;
  accountMonthlyPayment: number | null;
}

export function buildBudgetGridRows(
  payments: PaymentResponse[],
  accountsById: Map<number, AccountResponse>,
): BudgetGridRow[] {
  return payments.map((payment) => {
    const account = accountsById.get(payment.accountId);
    return {
      ...payment,
      accountName: account?.name ?? `Account ${payment.accountId}`,
      accountNumber: account?.number ?? "",
      accountCategoryId: account?.accountCategoryId ?? 0,
      accountBalance: account?.balance ?? 0,
      accountMonthlyPayment: account?.monthlyPayment ?? null,
    };
  });
}

export function toUpdatePaymentRequest(row: BudgetGridRow): UpdatePaymentRequest {
  return {
    budgetId: row.budgetId,
    accountId: row.accountId,
    paymentMade: row.paymentMade,
    amount: row.amount,
    budgetPaymentStatusId: row.budgetPaymentStatusId,
    isCleared: row.isCleared,
    paymentDate: row.paymentDate,
    clearedDate: row.clearedDate,
    paymentSourceId: row.paymentSourceId,
  };
}

export function getBudgetStatusRowClass(statusName: string): string {
  switch (statusName.trim().toLowerCase()) {
    case "pending":
    case "scheduled":
      return "budget-row-scheduled";
    case "paid":
      return "budget-row-paid";
    default:
      return "budget-row-alert";
  }
}
