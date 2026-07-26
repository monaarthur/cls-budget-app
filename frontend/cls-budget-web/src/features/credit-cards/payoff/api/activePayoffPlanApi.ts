import { apiDelete, apiGet, apiPost, apiPut, ApiError } from "@/lib/api/client";
import type {
  ActivatePayoffPlanRequest,
  ActivePayoffPlan,
  ActivePayoffPlanHistory,
  ActivePayoffPlanProgress,
  PayoffPlanPayment,
  RecordPayoffPlanPaymentRequest,
  ReviseActivePayoffPlanRequest,
} from "@/features/credit-cards/payoff/types";

export const activePayoffPlanApi = {
  async activate(body: ActivatePayoffPlanRequest) {
    const response = await apiPost<ActivePayoffPlan, ActivatePayoffPlanRequest>(
      "/api/v1/payoff-plans/activate",
      body,
    );
    if (!response.data) {
      throw new Error("Activate plan response was empty");
    }
    return response.data;
  },

  async getActive(): Promise<ActivePayoffPlan | null> {
    try {
      const response = await apiGet<ActivePayoffPlan>("/api/v1/payoff-plans/active");
      return response.data ?? null;
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        return null;
      }
      throw err;
    }
  },

  async revise(body: ReviseActivePayoffPlanRequest) {
    const response = await apiPut<ActivePayoffPlan, ReviseActivePayoffPlanRequest>(
      "/api/v1/payoff-plans/active",
      body,
    );
    if (!response.data) {
      throw new Error("Revise plan response was empty");
    }
    return response.data;
  },

  async recordPayment(body: RecordPayoffPlanPaymentRequest) {
    const response = await apiPost<PayoffPlanPayment, RecordPayoffPlanPaymentRequest>(
      "/api/v1/payoff-plans/active/payments",
      body,
    );
    if (!response.data) {
      throw new Error("Record payment response was empty");
    }
    return response.data;
  },

  async voidPayment(paymentId: number) {
    await apiDelete(`/api/v1/payoff-plans/active/payments/${paymentId}`);
  },

  async complete() {
    const response = await apiPost<ActivePayoffPlan, Record<string, never>>(
      "/api/v1/payoff-plans/active/complete",
      {},
    );
    return response.data ?? null;
  },

  async abandon() {
    const response = await apiPost<ActivePayoffPlan, Record<string, never>>(
      "/api/v1/payoff-plans/active/abandon",
      {},
    );
    return response.data ?? null;
  },

  async history(): Promise<ActivePayoffPlanHistory | null> {
    try {
      const response = await apiGet<ActivePayoffPlanHistory>(
        "/api/v1/payoff-plans/active/history",
      );
      return response.data ?? null;
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        return null;
      }
      throw err;
    }
  },

  async progress(): Promise<ActivePayoffPlanProgress | null> {
    try {
      const response = await apiGet<ActivePayoffPlanProgress>(
        "/api/v1/payoff-plans/active/progress",
      );
      return response.data ?? null;
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        return null;
      }
      throw err;
    }
  },
};
