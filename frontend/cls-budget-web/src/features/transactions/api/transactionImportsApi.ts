import { apiGet, apiPost, apiPut, apiUploadFile } from "@/lib/api/client";
import type {
  TransactionImportDetail,
  TransactionImportSummary,
} from "@/features/transactions/types/transactionImport";

const path = "/api/v1/transaction-imports";

export const transactionImportsApi = {
  getAll: () => apiGet<TransactionImportSummary[]>(path),
  getById: (id: number) => apiGet<TransactionImportDetail>(`${path}/${id}`),
  upload: (file: File) => apiUploadFile<TransactionImportDetail>(path, file),
  updateImport: (transactionImportId: number, body: { incomeSourceId: number | null }) =>
    apiPut<TransactionImportDetail, { incomeSourceId: number | null }>(
      `${path}/${transactionImportId}`,
      body,
    ),
};
