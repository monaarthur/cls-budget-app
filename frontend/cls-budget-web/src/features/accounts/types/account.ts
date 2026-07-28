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
  gracePeriod?: number | null;
  graceDay?: number | null;
  phone: string;
  email: string;
  url: string;
  username: string | null;
  notes: string | null;
  isPaidOff: boolean;
  paidOffDate: string | null;
  isCreditCard: boolean | null;
  accountCategoryId: number;
  accountCategoryName?: string | null;
  accountSubCategoryId?: number | null;
  accountSubCategoryName?: string | null;
  /** Purchase APR percent (e.g. 22.99). */
  interestRate: number | null;
  promotionalAnnualPercentageRate?: number | null;
  promotionalRateExpirationDate?: string | null;
  minimumPaymentPercentage?: number | null;
  minimumPaymentFloor?: number | null;
  /** Optional cash advance APR percent (e.g. 28.99). */
  cashOutInterestRate?: number | null;
  /** Optional cash advance fee percent (e.g. 5 = 5%). */
  cashAdvanceFeePercentage?: number | null;
  /** When false, omitted from multi-card payoff analysis. Defaults to true. */
  includeInPayoffAnalysis?: boolean;
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
  gracePeriod?: number | null;
  /** Calculated server-side from paymentDay + gracePeriod; optional on write. */
  graceDay?: number | null;
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
  accountSubCategoryId?: number | null;
  interestRate?: number | null;
  promotionalAnnualPercentageRate?: number | null;
  promotionalRateExpirationDate?: string | null;
  minimumPaymentPercentage?: number | null;
  minimumPaymentFloor?: number | null;
  /** Optional cash advance APR percent (e.g. 28.99). */
  cashOutInterestRate?: number | null;
  /** Optional cash advance fee percent (e.g. 5 = 5%). */
  cashAdvanceFeePercentage?: number | null;
  /** When false, omitted from multi-card payoff analysis. Defaults to true. */
  includeInPayoffAnalysis?: boolean;
}

export type UpdateAccountRequest = CreateAccountRequest;
