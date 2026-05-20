import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/client";
import type {
  CreatePaymentRequest,
  PaymentResponse,
  BudgetPaymentStatusResponse,
  UpdatePaymentRequest,
} from "@/features/payments/types/payment";

const paymentsPath = "/api/v1/payments";
const statusesPath = "/api/v1/budget-payment-statuses";

export const paymentsApi = {
  getAll: () => apiGet<PaymentResponse[]>(paymentsPath),
  getById: (id: number) => apiGet<PaymentResponse>(`${paymentsPath}/${id}`),
  create: (body: CreatePaymentRequest) =>
    apiPost<PaymentResponse, CreatePaymentRequest>(paymentsPath, body),
  update: (id: number, body: UpdatePaymentRequest) =>
    apiPut<PaymentResponse, UpdatePaymentRequest>(`${paymentsPath}/${id}`, body),
  remove: (id: number) => apiDelete<null>(`${paymentsPath}/${id}`),
  getStatuses: () => apiGet<BudgetPaymentStatusResponse[]>(statusesPath),
};
