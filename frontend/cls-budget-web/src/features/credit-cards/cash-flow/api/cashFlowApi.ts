import { apiPost } from "@/lib/api/client";
import type {
  AnalyzeCashFlowRequest,
  CalculationEnvelope,
  CashFlowAnalysisResult,
} from "@/features/credit-cards/cash-flow/types";

export const cashFlowApi = {
  async analyze(body: AnalyzeCashFlowRequest) {
    const response = await apiPost<
      CalculationEnvelope<CashFlowAnalysisResult>,
      AnalyzeCashFlowRequest
    >("/api/v1/cash-flow/analyze", body);
    if (!response.data) {
      throw new Error("Cash-flow analysis response was empty");
    }
    return response.data;
  },
};
