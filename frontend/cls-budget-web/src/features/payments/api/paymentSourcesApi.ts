import { apiGet } from "@/lib/api/client";
import type { PaymentSourceResponse } from "@/features/payments/types/payment";

const paymentSourcesPath = "/api/v1/payment-sources";

export const paymentSourcesApi = {
  getAll: () => apiGet<PaymentSourceResponse[]>(paymentSourcesPath),
};
