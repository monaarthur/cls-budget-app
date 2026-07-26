import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/client";
import type { CalculationEnvelope } from "@/features/credit-cards/payoff/types";
import type {
  CompareSavedPayoffPlansRequest,
  CompareSavedPayoffPlansResult,
  SavePayoffPlanRequest,
  SavedPayoffPlan,
  UpdateSavedPayoffPlanRequest,
} from "@/features/credit-cards/payoff/types";

export const payoffPlansApi = {
  async list() {
    const response = await apiGet<SavedPayoffPlan[]>("/api/v1/payoff-plans");
    return response.data ?? [];
  },

  async create(body: SavePayoffPlanRequest) {
    const response = await apiPost<SavedPayoffPlan, SavePayoffPlanRequest>(
      "/api/v1/payoff-plans",
      body,
    );
    if (!response.data) {
      throw new Error("Save plan response was empty");
    }
    return response.data;
  },

  async update(id: number, body: UpdateSavedPayoffPlanRequest) {
    const response = await apiPut<SavedPayoffPlan, UpdateSavedPayoffPlanRequest>(
      `/api/v1/payoff-plans/${id}`,
      body,
    );
    if (!response.data) {
      throw new Error("Update plan response was empty");
    }
    return response.data;
  },

  async remove(id: number) {
    await apiDelete(`/api/v1/payoff-plans/${id}`);
  },

  async compareSaved(body: CompareSavedPayoffPlansRequest) {
    const response = await apiPost<
      CalculationEnvelope<CompareSavedPayoffPlansResult>,
      CompareSavedPayoffPlansRequest
    >("/api/v1/payoff-plans/compare-saved", body);
    if (!response.data) {
      throw new Error("Compare saved plans response was empty");
    }
    return response.data;
  },
};
