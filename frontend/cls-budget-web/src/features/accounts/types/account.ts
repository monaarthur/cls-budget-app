export interface AccountResponse {
  accountId: number;
  name: string;
  number: string;
  description: string | null;
  balance: number;
  limit: number;
  accountOpenDate: string;
  monthlyPayment: number | null;
  paymentDay: number | null;
  phone: string;
  email: string;
  url: string;
  username: string | null;
  notes: string | null;
  isPaidOff: boolean;
  paidOffDate: string | null;
  isCreditCard: boolean | null;
  accountCategoryId: number;
  /** Purchase APR percent (e.g. 22.99). */
  interestRate: number | null;
}

export interface CreateAccountRequest {
  name: string;
  number: string;
  description?: string | null;
  balance: number;
  limit: number;
  accountOpenDate: string;
  monthlyPayment?: number | null;
  paymentDay?: number | null;
  phone: string;
  email: string;
  url: string;
  username?: string | null;
  password?: string | null;
  notes?: string | null;
  isPaidOff: boolean;
  paidOffDate?: string | null;
  isCreditCard?: boolean | null;
  accountCategoryId: number;
  interestRate?: number | null;
}

export type UpdateAccountRequest = CreateAccountRequest;
