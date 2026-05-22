export interface PaymentResponse {
  budgetPaymentId: number;
  budgetId: number;
  accountId: number;
  paymentMade: number;
  amount: number;
  budgetPaymentStatusId: number;
  budgetPaymentStatusName: string;
  isCleared: boolean;
  paymentDate: string;
  clearedDate: string | null;
  paymentSourceId: number | null;
}

export interface BudgetPaymentStatusResponse {
  budgetPaymentStatusId: number;
  name: string;
  description: string | null;
}

export interface CreatePaymentRequest {
  budgetId: number;
  accountId: number;
  paymentMade: number;
  amount: number;
  budgetPaymentStatusId: number;
  isCleared: boolean;
  paymentDate: string;
  clearedDate?: string | null;
  paymentSourceId?: number | null;
}

export type UpdatePaymentRequest = CreatePaymentRequest;
