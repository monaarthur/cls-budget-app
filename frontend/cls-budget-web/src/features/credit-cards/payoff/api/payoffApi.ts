import { apiGet, apiPost } from "@/lib/api/client";
import type {
  CalculationEnvelope,
  CompareLoanSavingsRequest,
  CompareLoanSavingsResult,
  ComparePayoffPlansRequest,
  ComparePayoffPlansResult,
  LoanScheduleRequest,
  LoanScheduleResult,
  UtilizationSummaryResult,
} from "@/features/credit-cards/payoff/types";

export const payoffApi = {
  async compare(body: ComparePayoffPlansRequest) {
    const response = await apiPost<
      CalculationEnvelope<ComparePayoffPlansResult>,
      ComparePayoffPlansRequest
    >("/api/v1/payoff-plans/compare", body);
    if (!response.data) {
      throw new Error("Compare response was empty");
    }
    return response.data;
  },

  async loanSavings(body: CompareLoanSavingsRequest) {
    const response = await apiPost<
      CalculationEnvelope<CompareLoanSavingsResult>,
      CompareLoanSavingsRequest
    >("/api/v1/payoff-plans/loan-savings", body);
    if (!response.data) {
      throw new Error("Loan savings response was empty");
    }
    return response.data;
  },

  async loanSchedule(body: LoanScheduleRequest) {
    const response = await apiPost<
      CalculationEnvelope<LoanScheduleResult>,
      LoanScheduleRequest
    >("/api/v1/credit-cards/loan-schedule", body);
    if (!response.data) {
      throw new Error("Loan schedule response was empty");
    }
    return response.data;
  },

  async utilizationSummary() {
    const response = await apiGet<
      CalculationEnvelope<UtilizationSummaryResult>
    >("/api/v1/credit-cards/utilization-summary");
    if (!response.data) {
      throw new Error("Utilization summary was empty");
    }
    return response.data;
  },
};
