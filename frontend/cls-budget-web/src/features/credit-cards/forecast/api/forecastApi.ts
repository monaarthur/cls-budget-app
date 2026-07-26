import { apiDelete, apiGet, apiPost } from "@/lib/api/client";
import type {
  CalculationEnvelope,
  CreateForecastRequest,
  ForecastResult,
} from "@/features/credit-cards/forecast/types";

export const forecastApi = {
  async create(body: CreateForecastRequest) {
    const response = await apiPost<
      CalculationEnvelope<ForecastResult>,
      CreateForecastRequest
    >("/api/v1/forecasts", body);
    if (!response.data) {
      throw new Error("Forecast response was empty");
    }
    return response.data;
  },

  async get(forecastId: number) {
    const response = await apiGet<CalculationEnvelope<ForecastResult>>(
      `/api/v1/forecasts/${forecastId}`,
    );
    if (!response.data) {
      throw new Error("Forecast response was empty");
    }
    return response.data;
  },

  async remove(forecastId: number) {
    await apiDelete(`/api/v1/forecasts/${forecastId}`);
  },
};
