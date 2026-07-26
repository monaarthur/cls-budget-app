import { apiPost } from "@/lib/api/client";
import type {
  AnalyzeBalanceTransferRequest,
  BalanceTransferAnalysisResult,
  CalculationEnvelope,
} from "@/features/credit-cards/balance-transfer/types";

export const balanceTransferApi = {
  async analyze(body: AnalyzeBalanceTransferRequest) {
    const response = await apiPost<
      CalculationEnvelope<BalanceTransferAnalysisResult>,
      AnalyzeBalanceTransferRequest
    >("/api/v1/balance-transfers/analyze", body);
    if (!response.data) {
      throw new Error("Balance transfer analysis response was empty");
    }
    return response.data;
  },
};
